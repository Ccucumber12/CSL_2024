#include "I2Cdev.h"
#include "MPU6050_6Axis_MotionApps20.h"
#if I2CDEV_IMPLEMENTATION == I2CDEV_ARDUINO_WIRE
    #include "Wire.h"
#endif
#include "ESP8266WiFi.h"
#include "WiFiClient.h"
MPU6050 mpu;
#define OUTPUT_READABLE_YAWPITCHROLL
#define INTERRUPT_PIN D5  // use pin D5 on nodeMC
#define LED_PIN 13 // (Arduino is 13, Teensy is 11, Teensy++ is 6)
bool blinkState = false;

// MPU control/status vars
bool dmpReady = false;  // set true if DMP init was successful
uint8_t mpuIntStatus;   // holds actual interrupt status byte from MPU
uint8_t devStatus;      // return status after each device operation (0 = success, !0 = error)
uint16_t packetSize;    // expected DMP packet size (default is 42 bytes)
uint16_t fifoCount;     // count of all bytes currently in FIFO
uint8_t fifoBuffer[64]; // FIFO storage buffer

// orientation/motion vars
Quaternion q;           // [w, x, y, z]         quaternion container
VectorInt16 aa;         // [x, y, z]            accel sensor measurements
VectorInt16 aaReal;     // [x, y, z]            gravity-free accel sensor measurements
VectorInt16 aaWorld;    // [x, y, z]            world-frame accel sensor measurements
VectorFloat gravity;    // [x, y, z]            gravity vector
float euler[3];         // [psi, theta, phi]    Euler angle container
float ypr[3];           // [yaw, pitch, roll]   yaw/pitch/roll container and gravity vector

// packet structure for InvenSense teapot demo
uint8_t teapotPacket[14] = { '$', 0x02, 0,0, 0,0, 0,0, 0,0, 0x00, 0x00, '\r', '\n' };

volatile bool mpuInterrupt = false;     // indicates whether MPU interrupt pin has gone high
void ICACHE_RAM_ATTR dmpDataReady() {
    mpuInterrupt = true;
}

const char* apName = "your_ap_name";
const char* apPassword = "your_ap_password";
IPAddress staticIP(192,168,128,1);
IPAddress gateway(192,168,128,1);
IPAddress subnet(255,255,255,0);
WiFiServer server(80);

void setup() {
    // join I2C bus (I2Cdev library doesn't do this automatically)
    #if I2CDEV_IMPLEMENTATION == I2CDEV_ARDUINO_WIRE
        Wire.begin();
        Wire.setClock(400000); // 400kHz I2C clock. Comment this line if having compilation difficulties
    #elif I2CDEV_IMPLEMENTATION == I2CDEV_BUILTIN_FASTWIRE
        Fastwire::setup(400, true);
    #endif

    Serial.begin(115200);
    while (!Serial); // wait for Leonardo enumeration, others continue immediately

    // initialize device
    Serial.println(F("Initializing I2C devices..."));
    mpu.initialize();
    pinMode(INTERRUPT_PIN, INPUT);

    // verify connection
    Serial.println(F("Testing device connections..."));
    Serial.println(mpu.testConnection() ? F("MPU6050 connection successful") : F("MPU6050 connection failed"));

    // wait for ready
    Serial.println(F("\nSend any character to begin DMP programming and demo: "));
    while (Serial.available() && Serial.read()); // empty buffer
    while (!Serial.available());                 // wait for data
    while (Serial.available() && Serial.read()); // empty buffer again

    // load and configure the DMP
    Serial.println(F("Initializing DMP..."));
    devStatus = mpu.dmpInitialize();

    // supply your own gyro offsets here, scaled for min sensitivity
    mpu.setXGyroOffset(220);
    mpu.setYGyroOffset(76);
    mpu.setZGyroOffset(-85);
    mpu.setZAccelOffset(1788); // 1688 factory default for my test chip

    // make sure it worked (returns 0 if so)
    if (devStatus == 0) {
        // turn on the DMP, now that it's ready
        Serial.println(F("Enabling DMP..."));
        mpu.setDMPEnabled(true);

        // enable Arduino interrupt detection
        Serial.print(F("Enabling interrupt detection (Arduino external interrupt "));
        Serial.print(digitalPinToInterrupt(INTERRUPT_PIN));
        Serial.println(F(")..."));
        attachInterrupt(digitalPinToInterrupt(INTERRUPT_PIN), dmpDataReady, RISING);
        mpuIntStatus = mpu.getIntStatus();

        // set our DMP Ready flag so the main loop() function knows it's okay to use it
        Serial.println(F("DMP ready! Waiting for first interrupt..."));
        dmpReady = true;

        // get expected DMP packet size for later comparison
        packetSize = mpu.dmpGetFIFOPacketSize();
    } else {
        // ERROR!
        // 1 = initial memory load failed
        // 2 = DMP configuration updates failed
        // (if it's going to break, usually the code will be 1)
        Serial.print(F("DMP Initialization failed (code "));
        Serial.print(devStatus);
        Serial.println(F(")"));
    }

    // configure LED for output
    pinMode(LED_PIN, OUTPUT);

    Serial.begin(115200);
    Serial.println();

    WiFi.mode(WIFI_AP);

    Serial.print("Setting soft-AP configuration ... ");
    Serial.println(WiFi.softAPConfig(staticIP, gateway, subnet) ? "Ready" : "Failed!");

    Serial.print("Setting soft-AP ... ");
    Serial.println(WiFi.softAP(apName, apPassword) ? "Ready" : "Failed!");

    Serial.print("Soft-AP IP address = ");
    Serial.println(WiFi.softAPIP());

    Serial.printf("Stations connected to soft-AP = %d\n", WiFi.softAPgetStationNum());

    // Initialize server
    server.begin();

    pinMode(D7,INPUT);
    pinMode(A0,INPUT);
}

void loop() {
    // put your main code here, to run repeatedly:
    WiFiClient client = server.available();
    if (!client){
      Serial.println("Wait for client...");
      delay(100);
      return ;
    }

    Serial.println("A new client is connected!");

    String data = "";
    while (client.connected()) {
      // if programming failed, don't try to do anything
      if (!dmpReady) return;

      // wait for MPU interrupt or extra packet(s) available
      while (!mpuInterrupt && fifoCount < packetSize) {
          if (mpuInterrupt && fifoCount < packetSize) {
            // try to get out of the infinite loop 
            fifoCount = mpu.getFIFOCount();
          }  
      }

      // reset interrupt flag and get INT_STATUS byte
      mpuInterrupt = false;
      mpuIntStatus = mpu.getIntStatus();

      // get current FIFO count
      fifoCount = mpu.getFIFOCount();

      // check for overflow (this should never happen unless our code is too inefficient)
      if ((mpuIntStatus & _BV(MPU6050_INTERRUPT_FIFO_OFLOW_BIT)) || fifoCount >= 1024) {
          // reset so we can continue cleanly
          mpu.resetFIFO();
          fifoCount = mpu.getFIFOCount();
          Serial.println(F("FIFO overflow!"));

      // otherwise, check for DMP data ready interrupt (this should happen frequently)
      } else if (mpuIntStatus & _BV(MPU6050_INTERRUPT_DMP_INT_BIT)) {
          // wait for correct available data length, should be a VERY short wait
          while (fifoCount < packetSize) fifoCount = mpu.getFIFOCount();

          // read a packet from FIFO
          mpu.getFIFOBytes(fifoBuffer, packetSize);
          
          // track FIFO count here in case there is > 1 packet available
          // (this lets us immediately read more without waiting for an interrupt)
          fifoCount -= packetSize;

          #ifdef OUTPUT_READABLE_YAWPITCHROLL
              // display Euler angles in degrees
              mpu.dmpGetQuaternion(&q, fifoBuffer);
              mpu.dmpGetGravity(&gravity, &q);
              mpu.dmpGetYawPitchRoll(ypr, &q, &gravity);
              Serial.print("(");
              client.print("(");

              Serial.print((analogRead(A0)-500.0)/(-500.0));
              client.print((analogRead(A0)-500.0)/(-500.0));
              Serial.print(",");
              client.print(",");

              //Serial.print(ypr[0] * 180/M_PI);
              //client.print(ypr[0] * 180/M_PI);
              //Serial.print(",");
              //client.print(",");
              //Serial.print(ypr[1] * 180/M_PI);
              //client.print(ypr[1] * 180/M_PI);
              //Serial.print(",");
              //client.print(",");
              Serial.print(-ypr[2] * 180/M_PI/50.0);
              client.print(-ypr[2] * 180/M_PI/50.0);

              Serial.print(",");
              client.print(",");
              Serial.print(digitalRead(D7));
              client.print(digitalRead(D7));
              Serial.print(")");
              client.print(")");
              Serial.println("");
          #endif

          // blink LED to indicate activity
          blinkState = !blinkState;
          digitalWrite(LED_PIN, blinkState);
      }
      delay(10);
    }
    client.stop();
    Serial.println("Client disconnected");
}