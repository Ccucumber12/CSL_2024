/***** IR Sensors *****/
#define SENSOR1 A4
#define SENSOR2 A5
#define SENSOR3 A6
#define SENSOR4 A7


/***** LEDs *****/
#define LED1W 2
#define LED1B 3
#define LED2W 4
#define LED2B 5
#define LED3W 6
#define LED3B 7
#define LED4W 14
#define LED4B 15


/***** DC Motor (L298N) *****/
#define MT1l 12
#define MT1r 13
#define PW1 11

#define MT2l 8
#define MT2r 9
#define PW2 10


/***** Sensor Constants *****/
const int WHITE_THRESH = 100;
const int BLACK_THRESH = 500;

#define WHITE 0
#define BLACK 1
#define GRAY 2


void setup() {
  // IR Sensors
  pinMode(SENSOR1, INPUT);
  pinMode(SENSOR2, INPUT);
  pinMode(SENSOR3, INPUT);
  pinMode(SENSOR4, INPUT);

  // LEDs
  pinMode(LED1W, OUTPUT);
  pinMode(LED1B, OUTPUT);
  pinMode(LED2W, OUTPUT);
  pinMode(LED2B, OUTPUT);
  pinMode(LED3W, OUTPUT);
  pinMode(LED3B, OUTPUT);
  pinMode(LED4W, OUTPUT);
  pinMode(LED4B, OUTPUT);
  
  // DC Motor w/ L298N control module
  pinMode(MT1l, OUTPUT);
  pinMode(MT1r, OUTPUT);
  pinMode(PW1, OUTPUT);
  setDirection(0);
  
  pinMode(MT2l, OUTPUT);
  pinMode(MT2r, OUTPUT);
  pinMode(PW2, OUTPUT);
  setDirection(0);
  
  Serial.begin(38400);

  digitalWrite(LED1W, LOW);
  digitalWrite(LED1B, LOW);
}


int value1;
int value2;
int value3;
int value4;


void loop() {
  readAllSensors();
}


void readAllSensors() {
  value1 = readSensor(SENSOR1, LED1W, LED1B);
  value2 = readSensor(SENSOR2, LED2W, LED2B);
  value3 = readSensor(SENSOR3, LED3W, LED3B);
  value4 = readSensor(SENSOR4, LED4W, LED4B);
}


int readSensor(uint8_t sensor_pin, uint8_t white_pin, uint8_t black_pin) {
  int value =  analogRead(sensor_pin);
  // Serial.println(value);

  int white_value = LOW;
  int black_value = LOW;
  int sensor_value = GRAY;

  if (value < WHITE_THRESH) {
    white_value = HIGH;
    sensor_value = WHITE;
  } else if (value > BLACK_THRESH) {
    black_value = HIGH;
    sensor_value = BLACK;
  }
  digitalWrite(white_pin, white_value);
  digitalWrite(black_pin, black_value);
  return sensor_value;
}


void setDirection(int dir){
  if (dir == 0){
    digitalWrite(MT1l, HIGH);
    digitalWrite(MT1r, LOW);
  } else if (dir == 1){
    digitalWrite(MT1l, LOW);
    digitalWrite(MT1r, HIGH);
  }
}

void loopThroughLeds() {
  digitalWrite(LED1W, HIGH);
  delay(1000); 
  digitalWrite(LED1W, LOW);
  digitalWrite(LED1B, HIGH);
  delay(1000);
  digitalWrite(LED1B, LOW);
  digitalWrite(LED2W, HIGH);
  delay(1000);
  digitalWrite(LED2W, LOW);
  digitalWrite(LED2B, HIGH);
  delay(1000);
  digitalWrite(LED2B, LOW);
  digitalWrite(LED3W, HIGH);
  delay(1000);
  digitalWrite(LED3W, LOW);
  digitalWrite(LED3B, HIGH);
  delay(1000);
  digitalWrite(LED3B, LOW);
  digitalWrite(LED4W, HIGH);
  delay(1000);
  digitalWrite(LED4W, LOW);
  digitalWrite(LED4B, HIGH);
  delay(1000);
  digitalWrite(LED4B, LOW);
  delay(1000);
}
