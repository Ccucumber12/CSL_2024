import cv2
import numpy as np
from typing import List, Tuple
import argparse
import pickle
import time

from const import *
from detector import Finger
from number_detector import NumberDetector
from gesture_detector import GestureDetector
from finger_detector import FingerDetector


def main(args):
    is_use_record = (args.record != None)
    is_display_slider = args.slider
    is_save_record = args.save

    cap = cv2.VideoCapture(0) # change to your webcam device index
    if is_display_slider:
        cv2.namedWindow('Threshold Sliders')
        nothing = lambda x : x
        cv2.createTrackbar('B', 'Threshold Sliders', 220, 254, nothing)
        cv2.createTrackbar('G', 'Threshold Sliders', 180, 254, nothing)
        cv2.createTrackbar('R', 'Threshold Sliders', 245, 254, nothing)

    number_detector = NumberDetector()
    gesture_detector = GestureDetector()
    finger_detector = FingerDetector()

    record = []
    if is_use_record:
        with open(f'{args.record}', 'rb') as f:
            record = pickle.load(f)
        frame_counter = 0

    start_time = time.time()
    while True:
        if is_use_record:
            frame = record[frame_counter]
            frame_counter += 1
            if frame_counter == len(record):
                frame_counter = 0
        else:
            ret, frame = cap.read()
            frame = frame[100:100+FRAME_HEIGHT, 150:150+FRAME_WIDTH]
            frame = cv2.flip(frame, 1)
            record.append(frame)

        cv2.imshow("raw", frame)
        b_origin, g_origin, r_origin = cv2.split(frame)
        if is_display_slider:
            b_threshold = cv2.getTrackbarPos('B', 'Threshold Sliders')
            g_threshold = cv2.getTrackbarPos('G', 'Threshold Sliders')
            r_threshold = cv2.getTrackbarPos('R', 'Threshold Sliders')
        else:
            b_threshold = B_THRESHOLD
            g_threshold = G_THRESHOLD
            r_threshold = R_THRESHOLD
        _, b = cv2.threshold(b_origin, b_threshold, 255, cv2.THRESH_BINARY)
        _, g = cv2.threshold(g_origin, g_threshold, 255, cv2.THRESH_BINARY)
        _, r = cv2.threshold(r_origin, r_threshold, 255, cv2.THRESH_BINARY)
        if is_display_slider:
            zeros = np.zeros(r.shape[:2], dtype='uint8')
            cv2.imshow("Blue", cv2.merge([b, zeros, zeros]))
            cv2.imshow("Green", cv2.merge([zeros, g, zeros]))
            cv2.imshow("Red", cv2.merge([zeros, zeros, r]))

        result = r
        display = cv2.cvtColor(result, cv2.COLOR_RGB2BGR)
        contours, hierarchy = cv2.findContours(result, cv2.RETR_TREE, cv2.CHAIN_APPROX_SIMPLE)
        cv2.drawContours(display, contours, -1, RED, 1)

        # retrieve fingers
        fingers: List[Finger] = []
        for cnt in contours:
            area = cv2.contourArea(cnt)
            if area < FINGER_AREA_THRESHOLD:
                continue
            (x, y), radius = cv2.minEnclosingCircle(cnt)
            x, y, radius = int(x), int(y), int(radius)
            display = cv2.circle(display, (x, y), radius, GREEN, 2)
            fingers.append(Finger((x, y), radius))
        cv2.imshow("Result", display)

        # detection
        number_detector.run(fingers)
        gesture_detector.run(fingers)
        finger_detector.run(fingers)

        # Press q to quit
        if cv2.waitKey(1) & 0xFF == ord('q'):
            break

        elapsed_time = time.time() - start_time
        if elapsed_time < FRAME_DELAY:
            time.sleep(FRAME_DELAY - elapsed_time)
        start_time = time.time()

    if is_save_record:
        with open(f'{args.save}', 'wb') as f:
            pickle.dump(record, f)
    cap.release()
    cv2.destroyAllWindows()


def parse_args():
    parser = argparse.ArgumentParser(description='FTIR Touchpad')
    parser.add_argument('--record', type=str, help='Use the record file as video input.')
    parser.add_argument('--save', type=str, help='Save the current video input.')
    parser.add_argument('--slider', action='store_true', help='Display the threshold slider window.')
    return parser.parse_args()


if __name__ == '__main__':
    args = parse_args()
    main(args)