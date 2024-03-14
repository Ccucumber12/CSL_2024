from detector import *
from const import *


ID_COLORS = [
    (0, 0, 255),     # Red
    (0, 255, 0),     # Green
    (255, 0, 0),     # Blue
    (0, 255, 255),   # Yellow
    (255, 255, 0),   # Cyan
    (255, 0, 255),   # Magenta
    (0, 165, 255),   # Orange
    (128, 0, 128),   # Purple
    (0, 255, 0),     # Lime
    (128, 128, 0)    # Teal
]


class FingerDetector(AbstractDetector):
	def __init__(self):
		super().__init__()
		self.id_pos = [None for _ in range(len(ID_COLORS))]

	def __str__(self):
		return "Finger ID Detector"
	
	def reset(self):
		super().reset()
		self.id_pos = [None for _ in range(len(ID_COLORS))]
	
	def draw_circles(self, fingers: list[Finger], ids: List[int]):
		log_error = lambda msg: print(f"\033[91m[Error] {msg}\033[0m")
		if len(fingers) != len(ids):
			log_error("fingers and ids have different length.")
		self.clear_frame()
		for finger, id in zip(fingers, ids):
			if type(id) != int:
				log_error("Predict ids not int type.")
				raise TypeError
			if not (0 <= id and id < len(ID_COLORS)):
				log_error(f"Predict ids out of range. Range: {0}-{len(ID_COLORS)}.")
				raise ValueError
			self.id_pos[id] = finger.pos
		for idx, pos in enumerate(self.id_pos):
			if pos is not None:
				cv2.circle(self.screen, pos, 20, ID_COLORS[idx], 10)
	
	def run(self, fingers: List[Finger]):
		fingers = self.filter_fingers(fingers)
		self.check_reset(fingers)
		prediction = self.detect(fingers)
		self.draw_circles(fingers, prediction)
		self.draw_text(f"Finger ID")
		self.display()
	
	def detect(self, fingers: List[Finger]) -> List[int]:
		# TODO
		return [i for i in range(len(fingers))]