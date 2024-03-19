# CSL Lab1 - FTIR Touchpad

Provide detections on a FTIR touchpad with OpenCV.

## Features

The following detections are implemented in this project.

- Realtime number detection
- Gesture detection
- Finger ID detection

[Demo Video](https://youtu.be/bGaYpATnB6o)

## Usage

#### 1. Install the Python packages:

```cmd
pip install -r requirements.txt
```

#### 2. Set the webcam index in `main.py`:

```python
# change to your webcam device index
cap = cv2.VideoCapture(0) 
```

#### 3. Run the script

```cmd
python main.py
```

## Options

```command
python main.py [OPTIONS]

-h, --help       show this help message and exit
--record RECORD  Use the record file as video input.
--save SAVE      Save the current video input.
--slider         Display the threshold slider window.
```

## Structure

```command
├── README.md                   // help
├── const.py                    // configuration constants
├── detector.py                 // base abstract class for detectors
├── finger_detector.py
├── gesture_detector.py
├── main.py                     // entry script
├── number_detector.py
├── report
│   ├── CSL_HW1_Report.md
│   └── CSL_HW1_Report.pdf
└── requirements.txt
```
