import cv2
import numpy as np

from number import get_number
from gesture import get_gesture
from finger import get_finger

IS_DISPLAY_SLIDER = False


def nothing(x):
	pass

def cv2_initialize():
	cap = cv2.VideoCapture(1)
	if IS_DISPLAY_SLIDER:
		cv2.namedWindow('Threshold Sliders')
		cv2.createTrackbar('B', 'Threshold Sliders', 220, 254, nothing)
		cv2.createTrackbar('G', 'Threshold Sliders', 180, 254, nothing)
		cv2.createTrackbar('R', 'Threshold Sliders', 245, 254, nothing)
	return cap


def show_rgb_image(b, g, r):
	zeros = np.zeros(r.shape[:2], dtype='uint8')
	cv2.imshow("Blue", cv2.merge([b, zeros, zeros]))
	cv2.imshow("Green", cv2.merge([zeros, g, zeros]))
	cv2.imshow("Red", cv2.merge([zeros, zeros, r]))


def main():
	cap = cv2_initialize()
	while(True):
		ret, frame = cap.read()
		frame = frame[80:450, 150:550]
		# cv2.imshow("raw", frame)
		
		b_origin, g_origin, r_origin = cv2.split(frame)

		if IS_DISPLAY_SLIDER:
			b_threshold = cv2.getTrackbarPos('B', 'Threshold Sliders')
			g_threshold = cv2.getTrackbarPos('G', 'Threshold Sliders')
			r_threshold = cv2.getTrackbarPos('R', 'Threshold Sliders')
		else:
			b_threshold = 220
			g_threshold = 180
			r_threshold = 245
		_, b = cv2.threshold(b_origin, b_threshold, 255, cv2.THRESH_BINARY)
		_, g = cv2.threshold(g_origin, g_threshold, 255, cv2.THRESH_BINARY)
		_, r = cv2.threshold(r_origin, r_threshold, 255, cv2.THRESH_BINARY)
		# show_rgb_image(b, g, r)

		# _, b_inv = cv2.threshold(b_origin, b_threshold, 255, cv2.THRESH_BINARY_INV)
		# result = cv2.bitwise_and(r, b_inv)
		result = r
		display = cv2.cvtColor(result, cv2.COLOR_RGB2BGR)

		contours, hierarchy = cv2.findContours(result, cv2.RETR_TREE, cv2.CHAIN_APPROX_SIMPLE)
		cv2.drawContours(display, contours, -1, (0, 0, 255), 1)

		# Iterate through each contour, check the area and find the center
		coords = []
		for cnt in contours:
			area = cv2.contourArea(cnt)
			(x, y), radius = cv2.minEnclosingCircle(cnt)
			if radius > 10:
				display = cv2.circle(display, (int(x), int(y)), int(radius), (0, 255, 0), 2)
				coords.append((x, y))
			
		print(coords)


		cv2.imshow("Result", display)
		# Press q to quit
		if cv2.waitKey(1) & 0xFF == ord('q'):
			break

	cap.release()
	cv2.destroyAllWindows()


if __name__ == '__main__':
	main()