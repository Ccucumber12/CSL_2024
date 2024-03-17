from enum import Enum, auto
import random

from detector import *
from const import *


'''
tap
double tap
long press

swipe
zoom in
zoom out
rotate
scroll
'''

'''
1 finger
---
tap
- ok
double tap
- ok
long press
- ok
- for continuous 1 sec
swipe
- one finger that moves with speed

---
2 fingers
scroll
- two finger with almost same distance, almost same slope, moves with speed
zoom in
- distance of two finger decreases, but slope is almost the same
zoom out
- distance of two finger increases, but slope is almost the same
rotate
- slope changes drastically
'''


'''
The following functions are from https://stackoverflow.com/questions/2827393/angles-between-two-n-dimensional-vectors-in-python
'''



class Gesture(Enum):
	TAP = auto()
	DOUBLE_TAP = auto()
	LONG_PRESS = auto()
	SCROLL = auto()
	SWIPE = auto()
	ZOOM_IN = auto()
	ZOOM_OUT = auto()
	ROTATE = auto()
	# ADD NO_ACTION
	NO_ACTION = auto()

def unit_vector(vector):
	""" Returns the unit vector of the vector.  """
	return vector / np.linalg.norm(vector)

def angle_between(v1, v2):
	""" Returns the angle in radians between vectors 'v1' and 'v2'::
			>>> angle_between((1, 0, 0), (0, 1, 0))
			1.5707963267948966
			>>> angle_between((1, 0, 0), (1, 0, 0))
			0.0
			>>> angle_between((1, 0, 0), (-1, 0, 0))
			3.141592653589793
	"""
	v1_u = unit_vector(v1)
	v2_u = unit_vector(v2)
	return np.arccos(np.clip(np.dot(v1_u, v2_u), -1.0, 1.0))

def distance_between(v1, v2):
	return np.linalg.norm(v1 - v2)

def comb(a, b):
	if np.isnan(a):
		return b
	return a * FADE_COEF + b * (1 - FADE_COEF)

class GestureDetector(AbstractDetector):
	def __init__(self):
		super().__init__()
		self.cnt = 0

		self.last_tap_gap = DOUBLE_TAP_GAP
		self.last_vector = -1
		self.last_pos = -1
		self.cnt_touch = 0
		self.cnt_no_touch = 0
		self.ret = Gesture.NO_ACTION
		self.avg_angle = np.nan
		self.avg_zoom_delta = np.nan
		self.avg_move_delta = np.nan

	def __str__(self):
		return "Gesture Detector"
	
	def _fire_tap(self):
		if self.last_tap_gap <= DOUBLE_TAP_GAP:
			self.res = Gesture.DOUBLE_TAP
		else:
			self.res = Gesture.TAP

		self.last_tap_gap = 0

	def run(self, fingers: List[Finger]):
		fingers = self.filter_fingers(fingers)
		self.check_reset(fingers)
		self.draw_circles(fingers)
		prediction = self.detect(fingers)
		self.draw_text(f"Gesture: {prediction.name}")
		self.display()
	
	def detect(self, fingers: List[Finger]) -> Gesture:
		self.cnt += 1



		self.last_tap_gap += 1
		self.res = Gesture.NO_ACTION

		if len(fingers) == 0:
			self.last_pos = -1
			self.last_vector = -1
			self.last_pos = -1

			if 0 < self.cnt_touch <= TAP_LAST_FRAME:
				self._fire_tap()

			self.cnt_touch = 0
			self.cnt_no_touch += 1

			if self.cnt_no_touch >= RESET_FRAME_THRESHOLD:
				self.avg_angle = np.nan
				self.avg_zoom_delta = np.nan
				self.avg_move_delta = np.nan
		else:
			self.cnt_touch += 1
			self.cnt_no_touch = 0

			cur_pos = np.array(fingers[0].pos)
			cur_vector = np.array(fingers[0].pos) - np.array(fingers[-1].pos)

			if isinstance(self.last_pos, int):
				self.last_pos = cur_pos
			if isinstance(self.last_vector, int):
				self.last_vector = cur_vector

			cur_wid = np.linalg.norm(cur_vector)
			last_wid = np.linalg.norm(self.last_vector)

			if len(fingers) == 1:
				if distance_between(cur_pos, self.last_pos) > MOVEMENT_DELTA_THRESHOLD:
					self.ret = Gesture.SWIPE
				elif self.cnt_touch >= LONG_PRESS_THRESHOLD:
					self.ret = Gesture.LONG_PRESS

			self.avg_angle = comb(self.avg_angle, angle_between(cur_vector, self.last_vector))

			self.avg_move_delta = comb(self.avg_move_delta, distance_between(cur_pos, self.last_pos))

			if len(fingers) == 2:
				self.avg_zoom_delta = comb(self.avg_zoom_delta, cur_wid - last_wid)
				self.cnt_touch = -1

				print(f"Time: {self.cnt}\n" +
					  f"avg angle: {self.avg_angle}\n" +
					  f"avg zoom dis: {self.avg_zoom_delta}\n" +
					  f"avg move dis: {self.avg_move_delta}\n" + 
						"-----------------------------")
#				print(f"angle: {angle_between(cur_vector, self.last_vector)}\n" +
#					  f"zoom dis: {cur_wid - last_wid}\n" +
#					  f"move dis: {distance_between(cur_pos, self.last_pos)}\n" + 
#						"-----------------------------")
				if self.avg_angle > ROTATE_DELTA_THRESHOLD:
					self.ret = Gesture.ROTATE
				elif self.avg_zoom_delta > ZOOM_DELTA_THRESHOLD:
					self.ret = Gesture.ZOOM_OUT
				elif -self.avg_zoom_delta > ZOOM_DELTA_THRESHOLD:
					self.ret = Gesture.ZOOM_IN
				elif self.avg_move_delta > MOVEMENT_DELTA_THRESHOLD:
					self.ret = Gesture.SCROLL
				self.last_vector = cur_vector
#				if angle_between(cur_vector, self.last_vector) > ROTATE_DELTA_THRESHOLD:
#					self.ret = Gesture.ROTATE
#				elif cur_wid - last_wid > ZOOM_DELTA_THRESHOLD:
#					self.ret = Gesture.ZOOM_OUT
#				elif last_wid - cur_wid > ZOOM_DELTA_THRESHOLD:
#					self.ret = Gesture.ZOOM_IN
#				elif distance_between(cur_pos, self.last_pos) > MOVEMENT_DELTA_THRESHOLD:
#					self.ret = Gesture.SCROLL
			self.last_pos = cur_pos
#		if 'ret' not in self.__dict__ or random.randint(0, 200) == 0:
#			self.ret = random.choice(list(Gesture))
		return self.ret
