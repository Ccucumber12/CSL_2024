import random

from detector import *
from const import *

class NumberDetector(AbstractDetector):
    def __init__(self):
        super().__init__()

        self.RADIUS=FRAME_WIDTH/4
        self.SLOPE=0.4
        self.cnt_reset=0
        self.history=[]
        self.shapes=[
                [2,1,4,7,8,9,6,3,2],
                [2,5,8],
                [1,2,3,6,9,8,7,8,9],
                [1,2,3,6,5,6,9,8,7],
                [1,4,5,6,2,5,8],
                [1,2,3,1,4,5,6,9,8,7],
                [2,1,4,7,8,9,6,5,4],
                [1,2,3,5,7],
                [3,2,1,5,9,8,7,5,3],
                [3,2,1,4,5,6,3,6,9]]

    def __str__(self):
        return "Number Detector"

    def run(self, fingers: List[Finger]):
        fingers = self.filter_fingers(fingers)
        self.check_reset(fingers)
        self.draw_circles(fingers)
        prediction = self.detect(fingers)
        self.draw_text(f"Prediction: {prediction}")
        self.display()

    def filter_fingers(self,fingers):
        return [max(fingers,key=lambda x:x.radius)]

    def quadrant(self,finger):
        x=finger.pos[0]-FRAME_WIDTH/2
        y=finger.pos[1]-FRAME_HEIGHT/2
        if x**2+y**2<=self.RADIUS**2:
            return 5
        if x*self.SLOPE<=y: # Down Down Left
            if x*(-self.SLOPE)<=y: # Down Down Right
                if x*(1/self.SLOPE)<=y: # Down Left Left
                    if x*(-1/self.SLOPE)<=y: # Down Right Right
                        return 8
                    else:
                        return 7
                else:
                    return 9
            else:
                return 4
        else:
            if x*(-self.SLOPE)>=y: # Up Up Left
                if x*(1/self.SLOPE)>=y: # Up Right Right
                    if x*(-1/self.SLOPE)>=y: # Up Left Left
                        return 2
                    else:
                        return 3
                else:
                    return 1
            else:
                return 6

    def detect(self, fingers: List[Finger]) -> int:
        # TODO
        if len(fingers)==0:
            self.cnt_reset+=1
            if self.cnt_reset>=RESET_FRAME_THRESHOLD:
                self.history=[]
        else:
            fingers=self.filter_fingers(fingers)
            self.cnt_reset=0
            quadrant=self.quadrant(fingers[0])
            if self.history==[] or self.history[-1]!=quadrant:
                self.history.append(quadrant)
        for i in range(10):
            flag=True
            for j in range(len(self.history)):
                if len(self.history)>len(self.shapes[i]) or self.history[j]!=self.shapes[i][j]:
                    flag=False
                    break
            if flag:
                return i
        return None

        if 'ret' not in self.__dict__ or random.randint(0, 200) == 0:
            self.ret = random.randint(0, 9)
        return self.ret
