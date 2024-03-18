import cv2
import numpy as np
from typing import List, Tuple

from const import *


class Finger:
    def __init__(self, pos: Tuple[int, int], radius: int):
        self.pos = pos
        self.radius = radius


class AbstractDetector():
    def __init__(self):
        RGB_CHANNEL = 3
        self.screen = np.zeros((FRAME_HEIGHT+TEXT_BAR_HEIGHT, FRAME_WIDTH, RGB_CHANNEL), dtype='uint8')
        self.empty_frame_count = 0

    def __str__(self):
        return "Abstract Detector"

    def check_reset(self, fingers: List[Finger]):
        if len(fingers) == 0:
            self.empty_frame_count += 1
        else:
            self.empty_frame_count = 0
        if self.empty_frame_count >= RESET_FRAME_THRESHOLD:
            self.reset()

    def reset(self):
        self.clear_frame()

    def clear_frame(self):
        cv2.rectangle(self.screen, (0, 0), (FRAME_WIDTH, FRAME_HEIGHT), BLACK, -1)

    def clear_text(self):
        cv2.rectangle(self.screen, (0, FRAME_HEIGHT), (FRAME_WIDTH, FRAME_HEIGHT+TEXT_BAR_HEIGHT), BLACK, -1)

    def display(self):
        cv2.imshow(str(self), self.screen)

    def draw_circles(self, fingers: Finger | List[Finger]):
        if type(fingers) != list:
            fingers = [fingers]
        for finger in fingers:
            cv2.circle(self.screen, finger.pos, 10, WHITE, -1)

    def draw_text(self, text: str):
        self.clear_text()
        font_face = cv2.FONT_HERSHEY_SIMPLEX
        cv2.putText(self.screen, text, (10, FRAME_HEIGHT+TEXT_BAR_HEIGHT-15), font_face, 1, WHITE, 2)

    def filter_fingers(self, fingers: List[Finger]) -> List[Finger]:
        return fingers

    def run(self, fingers: List[Finger]):
        fingers = self.filter_fingers(fingers)
        self.check_reset(fingers)
        self.draw_circles(fingers)
        self.detect(fingers)
        self.draw_text("Abstract Detector")
        self.display()

    def detect(self, fingers: List[Finger]) -> any:
        raise NotImplementedError