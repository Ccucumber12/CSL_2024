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

#define LEFT 1
#define RIGHT 2
#define FRONT 1
#define BACK 2

/***** Sensor Constants *****/
const int WHITE_THRESH[4] = { 200, 350, 350, 350 };
const int BLACK_THRESH[4] = { 600, 800, 800, 650 };

#define WHITE 0
#define BLACK 1
#define GRAY 2


/***** Grid Counter *****/

const int STOP_NUMBER = 20;
const int CONTINUOUS_THRESH = 3;
int current_color = WHITE;
int stop_count = 0;
int change_count = 0;

const int IDLE_THRESH = 100;
int idle_count = 0;
int prev1, prev2, prev3, prev4;

const int TICK = 10;
const int BOOST = 100;

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
  setDirection(LEFT, FRONT);

  pinMode(MT2l, OUTPUT);
  pinMode(MT2r, OUTPUT);
  pinMode(PW2, OUTPUT);
  setDirection(RIGHT, FRONT);

  Serial.begin(38400);
}


int value1;
int value2;
int value3;
int value4;


#define POWER 80
#define SMALL_TURN_POWER 120
#define BIG_TURN_POWER 240

// #define POWER 50
// #define TURN_POWER 150

void loop() {
  // readAllSensors();
  // loopThroughSensors();
  // delay(200);
  drive();
  // turnLeft();
  // handleStop();
}


#define GO_STRAIGHT 0
#define TURN_LEFT 1
#define TURN_RIGHT 2

int previous_move = GO_STRAIGHT;

void drive() {
  recordPrev();
  readAllSensors();

  handleIdle();
  if (prev1 != GRAY && value1 != GRAY) {
    turnLeftBig();
  } else if (prev4 != GRAY && value4 != GRAY) {
    turnRightBig();
  } else {
    if (value2 == GRAY && value3 == GRAY) {
      if (previous_move == GO_STRAIGHT)
        goStraight();
      else if (previous_move == TURN_LEFT)
        turnLeftBig();
      else if (previous_move == TURN_RIGHT)
        turnRightBig();
    } else if (value3 == GRAY) {
      turnLeftSmall();
    } else if (value2 == GRAY) {
      turnRightSmall();
    } else {
      goStraight();
    }
  }
  handleColor();
  delay(TICK);
  /*
  if (previous_move != GO_STRAIGHT) {
    goStraight();
    handleColor();
    delay(TICK);
  }
  */
}

void handleColor() {
  int new_color = getColor();
  if (new_color != current_color) {
    change_count += 1;
  } else {
    change_count = max(change_count - 1, 0);
  }
  if (change_count == CONTINUOUS_THRESH) {
    change_count = 0;
    current_color = new_color;
    stop_count += 1;
    if (stop_count == STOP_NUMBER) {
      stop_count = 0;
      handleStop();

      setDirection(LEFT, FRONT);
      setPower(LEFT, 255);
      setDirection(RIGHT, FRONT);
      setPower(RIGHT, 255);
      delay(BOOST);
    }
  }
}

void handleStop() {
  setPower(LEFT, 0);
  setPower(RIGHT, 0);

  int gap = 3000 / 8 / 3;

  for (int i = 0; i < 3; i++) {
    delay(gap);

    setLight(2, HIGH);
    setLight(3, HIGH);
    delay(gap);

    setLight(1, HIGH);
    setLight(4, HIGH);
    delay(gap);

    setLight(5, HIGH);
    setLight(8, HIGH);
    delay(gap);

    setLight(6, HIGH);
    setLight(7, HIGH);
    delay(gap);

    setLight(6, LOW);
    setLight(7, LOW);
    delay(gap);

    setLight(5, LOW);
    setLight(8, LOW);
    delay(gap);

    setLight(1, LOW);
    setLight(4, LOW);
    delay(gap);

    setLight(2, LOW);
    setLight(3, LOW);
  }
}

void handleIdle() {
  if (prev1 == value1 && prev2 == value2 && prev3 == value3 && prev4 == value4) {
    idle_count += 1;
  } else {
    idle_count = 0;
  }
  if (idle_count == IDLE_THRESH) {
    idle_count = 0;
    setDirection(LEFT, FRONT);
    setPower(LEFT, 255);
    setDirection(RIGHT, FRONT);
    setPower(RIGHT, 255);
    delay(BOOST);
  }
}

void setLight(int index, int value) {
  if (index == 1)
    digitalWrite(LED4B, value);
  if (index == 2)
    digitalWrite(LED3B, value);
  if (index == 3)
    digitalWrite(LED2B, value);
  if (index == 4)
    digitalWrite(LED1B, value);
  if (index == 5)
    digitalWrite(LED4W, value);
  if (index == 6)
    digitalWrite(LED3W, value);
  if (index == 7)
    digitalWrite(LED2W, value);
  if (index == 8)
    digitalWrite(LED1W, value);
}

void recordPrev() {
  prev1 = value1;
  prev2 = value2;
  prev3 = value3;
  prev4 = value4;
}

int getColor() {
  int black_count = 0;
  int white_count = 0;
  black_count += (value1 == BLACK);
  black_count += (value2 == BLACK);
  black_count += (value3 == BLACK);
  black_count += (value4 == BLACK);
  white_count += (value1 == WHITE);
  white_count += (value2 == WHITE);
  white_count += (value3 == WHITE);
  white_count += (value4 == WHITE);

  if (black_count > white_count)
    return WHITE;
  if (black_count < white_count)
    return BLACK;
  return current_color;
}

void goStraight() {
  setDirection(LEFT, FRONT);
  setPower(LEFT, POWER);
  setDirection(RIGHT, FRONT);
  setPower(RIGHT, POWER);
  previous_move = GO_STRAIGHT;
}

void turnLeftSmall() {
  setDirection(LEFT, BACK);
  setPower(LEFT, SMALL_TURN_POWER);
  setDirection(RIGHT, FRONT);
  setPower(RIGHT, SMALL_TURN_POWER);
  previous_move = TURN_LEFT;
}

void turnRightSmall() {
  setDirection(LEFT, FRONT);
  setPower(LEFT, SMALL_TURN_POWER);
  setDirection(RIGHT, BACK);
  setPower(RIGHT, SMALL_TURN_POWER);
  previous_move = TURN_RIGHT;
}

void turnLeftBig() {
  setDirection(LEFT, BACK);
  setPower(LEFT, BIG_TURN_POWER);
  setDirection(RIGHT, FRONT);
  setPower(RIGHT, BIG_TURN_POWER);
  previous_move = TURN_LEFT;
}

void turnRightBig() {
  setDirection(LEFT, FRONT);
  setPower(LEFT, BIG_TURN_POWER);
  setDirection(RIGHT, BACK);
  setPower(RIGHT, BIG_TURN_POWER);
  previous_move = TURN_RIGHT;
}

void runMotor() {
  setDirection(LEFT, FRONT);
  setDirection(RIGHT, FRONT);

  analogWrite(PW1, POWER);
  analogWrite(PW2, POWER);

  delay(2000);

  analogWrite(PW1, 0);
  analogWrite(PW2, 0);
  setDirection(LEFT, BACK);
  setDirection(RIGHT, BACK);

  delay(2000);

  analogWrite(PW1, POWER);
  analogWrite(PW2, POWER);

  delay(2000);

  analogWrite(PW1, 0);
  analogWrite(PW2, 0);

  delay(2000);
}


void readAllSensors() {
  value1 = readSensor(SENSOR1, LED1W, LED1B);
  value2 = readSensor(SENSOR2, LED2W, LED2B);
  value3 = readSensor(SENSOR3, LED3W, LED3B);
  value4 = readSensor(SENSOR4, LED4W, LED4B);
}


int readSensor(uint8_t sensor_pin, uint8_t white_pin, uint8_t black_pin) {
  int value = analogRead(sensor_pin);
  Serial.println(value);

  int white_value = LOW;
  int black_value = LOW;
  int sensor_value = GRAY;

  if (value < getWhiteThresh(sensor_pin)) {
    white_value = HIGH;
    sensor_value = WHITE;
  } else if (value > getBlackThresh(sensor_pin)) {
    black_value = HIGH;
    sensor_value = BLACK;
  }
  digitalWrite(white_pin, white_value);
  digitalWrite(black_pin, black_value);
  return sensor_value;
}

int getWhiteThresh(uint8_t sensor_pin) {
  if (sensor_pin == SENSOR1)
    return WHITE_THRESH[0];
  if (sensor_pin == SENSOR2)
    return WHITE_THRESH[1];
  if (sensor_pin == SENSOR3)
    return WHITE_THRESH[2];
  if (sensor_pin == SENSOR4)
    return WHITE_THRESH[3];
  Serial.println("[ERROR] SENSOR NOT FOUND");
}

int getBlackThresh(uint8_t sensor_pin) {
  if (sensor_pin == SENSOR1)
    return BLACK_THRESH[0];
  if (sensor_pin == SENSOR2)
    return BLACK_THRESH[1];
  if (sensor_pin == SENSOR3)
    return BLACK_THRESH[2];
  if (sensor_pin == SENSOR4)
    return BLACK_THRESH[3];
  Serial.println("[ERROR] SENSOR NOT FOUND");
}


void setDirection(int side, int dir) {
  if (side == LEFT) {
    if (dir == FRONT) {
      digitalWrite(MT1l, LOW);
      digitalWrite(MT1r, HIGH);
    } else if (dir == BACK) {
      digitalWrite(MT1l, HIGH);
      digitalWrite(MT1r, LOW);
    } else {
      Serial.println("[ERROR] DIRECTION NOT FOUND");
    }
  } else if (side == RIGHT) {
    if (dir == FRONT) {
      digitalWrite(MT2l, HIGH);
      digitalWrite(MT2r, LOW);
    } else if (dir == BACK) {
      digitalWrite(MT2l, LOW);
      digitalWrite(MT2r, HIGH);
    } else {
      Serial.println("[ERROR] DIRECTION NOT FOUND");
    }
  } else {
    Serial.println("[ERROR] MOTOR NOT FOUND");
  }
}

void setPower(int direction, int power) {
  if (direction == LEFT) {
    //analogWrite(PW1, min(255, power * 1.3));
    analogWrite(PW1, power);
  } else if (direction == RIGHT) {
    analogWrite(PW2, power);
  } else {
    Serial.println("[ERROR] MOTOR NOT FOUND");
  }
}

void loopThroughLeds2() {
  digitalWrite(LED1W, HIGH);
  digitalWrite(LED1B, HIGH);
  delay(1000);
  digitalWrite(LED1B, LOW);
  digitalWrite(LED1W, LOW);
  digitalWrite(LED2W, HIGH);
  digitalWrite(LED2B, HIGH);
  delay(1000);
  digitalWrite(LED2W, LOW);
  digitalWrite(LED2B, LOW);
  digitalWrite(LED3W, HIGH);
  digitalWrite(LED3B, HIGH);
  delay(1000);
  digitalWrite(LED3W, LOW);
  digitalWrite(LED3B, LOW);
  digitalWrite(LED4W, HIGH);
  digitalWrite(LED4B, HIGH);
  delay(1000);
  digitalWrite(LED4W, LOW);
  digitalWrite(LED4B, LOW);
  delay(1000);
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

void loopThroughSensors() {
  Serial.print(analogRead(SENSOR1));
  Serial.print(" ");
  Serial.print(analogRead(SENSOR2));
  Serial.print(" ");
  Serial.print(analogRead(SENSOR3));
  Serial.print(" ");
  Serial.print(analogRead(SENSOR4));
  Serial.println();
}
