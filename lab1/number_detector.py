import random

from detector import *
from const import *


class NumberDetector(AbstractDetector):
    def __init__(self):
        super().__init__()

    def __str__(self):
        return "Number Detector"

    def run(self, fingers: List[Finger]):
        fingers = self.filter_fingers(fingers)
        self.check_reset(fingers)
        self.draw_circles(fingers)
        prediction = self.detect(fingers)
        self.draw_text(f"Prediction: {prediction}")
        self.display()

    def detect(self, fingers: List[Finger]) -> int:
        # TODO
        if 'ret' not in self.__dict__ or random.randint(0, 200) == 0:
            self.ret = random.randint(0, 9)
        return self.ret