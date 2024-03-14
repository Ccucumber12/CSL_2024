from enum import Enum, auto
import random

from detector import *
from const import *


class Gesture(Enum):
    TAP = auto()
    DOUBLE_TAP = auto()
    LONG_PRESS = auto()
    SCROLL = auto()
    SWIPE = auto()
    ZOOM_IN = auto()
    ZOOM_OUT = auto()
    ROTATE = auto()


class GestureDetector(AbstractDetector):
	def __init__(self):
		super().__init__()

	def __str__(self):
		return "Gesture Detector"
	
	def run(self, fingers: List[Finger]):
		fingers = self.filter_fingers(fingers)
		self.check_reset(fingers)
		self.draw_circles(fingers)
		prediction = self.detect(fingers)
		self.draw_text(f"Gesture: {prediction.name}")
		self.display()
	
	def detect(self, fingers: List[Finger]) -> Gesture:
		# TODO
		if 'ret' not in self.__dict__ or random.randint(0, 200) == 0:
			self.ret = random.choice(list(Gesture))
		return self.ret