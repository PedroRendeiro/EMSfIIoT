import configparser
import argparse

import numpy as np
import imutils
import cv2
from imutils import contours
try:
    from picamera import PiCamera
    from time import sleep
except:
    pass

import lib.classImageAcquisition as ImageAcquisition

class ImageClassification:
    """
    ImageClassification Class

    Reads number from image with template matching

    Attributes:
        reference: Reference template path
        image:  Image to process path
        rectKernel: Morphological structural element, rectangle
        sqKernel: Morphological structural element, square
        digits: Digits retrived from template
    """

    def __init__(self):
        """
        Class constructor

        Reads configuration file and sets variables.
        Reads arguments from input command.
        Processes and stores template image
        """
        config = configparser.ConfigParser()
        config.read("./config/config.ini")        

        if config.has_option('source', 'device'):
            if config['source']['device'] == "ESP32_CAM":
                url = config['source']['ip']
                self.ImageAcquisition =  ImageAcquisition.ImageAcquisition()
                self.ImageAcquisition.LoadImageFromURL(url, './images/test.png')
            elif config['source']['device'] == "RaspiCam":
                camera = PiCamera()
                camera.start_preview()
                sleep(2)
                camera.capture('./images/test.png')
                camera.stop_preview()
            elif config['source']['device'] == "WebCam":
                pass
            self.image = cv2.imread('./images/test.png')
        else:
            # construct the argument parse and parse the arguments
            ap = argparse.ArgumentParser()
            ap.add_argument("-i", "--image", required=True,
                help="path to input image")
            ap.add_argument("-r", "--reference", required=False,
                help="path to reference OCR-A image")
            args = vars(ap.parse_args())

            self.image = cv2.imread(args["image"])

        self.reference = config['reference']['path']

        # initialize a rectangular (wider than it is tall) and square
        # structuring kernel
        self.rectKernel = cv2.getStructuringElement(cv2.MORPH_RECT, (9, 3))
        self.sqKernel = cv2.getStructuringElement(cv2.MORPH_RECT, (2, 2))
        
        # load the reference OCR-A image from disk, convert it to grayscale,
        # and threshold it, such that the digits appear as *white* on a
        # *black* background
        # and invert it, such that the digits appear as *white* on a *black*
        ref = cv2.imread(self.reference)
        ref = cv2.cvtColor(ref, cv2.COLOR_BGR2GRAY)
        ref = cv2.threshold(ref, 10, 255, cv2.THRESH_BINARY_INV)[1]
        rectKernel = cv2.getStructuringElement(cv2.MORPH_RECT, (9, 9))
        ref = cv2.morphologyEx(ref, cv2.MORPH_CLOSE, rectKernel)
        
        # find contours in the OCR-A image (i.e,. the outlines of the digits)
        # sort them from left to right, and initialize a dictionary to map
        # digit name to the ROI
        refCnts = cv2.findContours(ref.copy(), cv2.RETR_EXTERNAL,
            cv2.CHAIN_APPROX_SIMPLE)
        refCnts = imutils.grab_contours(refCnts)
        refCnts = contours.sort_contours(refCnts, method="left-to-right")[0]
        self.digits = {}

        # loop over the OCR-A reference contours
        for (i, c) in enumerate(refCnts):
            # compute the bounding box for the digit, extract it, and resize
            # it to a fixed size
            (x, y, w, h) = cv2.boundingRect(c)
            roi = ref[y:y + h, x:x + w]
            roi = cv2.resize(roi, (57, 88))

            # update the digits dictionary, mapping the digit name to the ROI
            self.digits[i] = roi

    def readImage(self, showResult = False):
        """
        Read digits from image function
        """
        # resize the input image, and convert it to grayscale
        image = imutils.resize(self.image, width=300)
        image = imutils.rotate(image, -90)
        gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)

        if showResult:
            cv2.imshow("Image", gray)
            cv2.waitKey(0)
            cv2.destroyAllWindows()

        # apply a tophat (blackhat) morphological operator
        tophat = cv2.morphologyEx(gray, cv2.MORPH_BLACKHAT, self.rectKernel)
        #tophat = cv2.equalizeHist(tophat)
        tophat[tophat<40] = 0
        rectKernel = cv2.getStructuringElement(cv2.MORPH_RECT, (18, 6))
        tophat = cv2.morphologyEx(tophat, cv2.MORPH_CLOSE, rectKernel)

        if showResult:
            cv2.imshow("Image", tophat)
            cv2.waitKey(0)
            cv2.destroyAllWindows()

        # compute the Scharr gradient of the tophat image, then scale
        # the rest back into the range [0, 255]
        gradX = cv2.Sobel(tophat, ddepth=cv2.CV_32F, dx=1, dy=0, ksize=-1)
        gradX = np.absolute(gradX)
        (minVal, maxVal) = (np.min(gradX), np.max(gradX))
        gradX = (255 * ((gradX - minVal) / (maxVal - minVal)))
        gradX = gradX.astype("uint8")

        # apply a closing operation using the rectangular kernel to help
        # cloes gaps in between credit card number digits, then apply
        # Otsu's thresholding method to binarize the image
        gradX = cv2.morphologyEx(tophat, cv2.MORPH_CLOSE, self.rectKernel)
        
        if showResult:
            cv2.imshow("Image", gradX)
            cv2.waitKey(0)
            cv2.destroyAllWindows()        
        
        thresh = cv2.threshold(gradX, 0, 255,
            cv2.THRESH_BINARY | cv2.THRESH_TRIANGLE)[1]

        if showResult:
            cv2.imshow("Image", thresh)
            cv2.waitKey(0)
            cv2.destroyAllWindows()    

        # apply a second closing operation to the binary image, again
        # to help close gaps between credit card number regions
        thresh = cv2.morphologyEx(thresh, cv2.MORPH_CLOSE, self.sqKernel)

        if showResult:
            cv2.imshow("Image", thresh)
            cv2.waitKey(0)
            cv2.destroyAllWindows() 

        # find contours in the thresholded image, then initialize the
        # list of digit locations
        cnts = cv2.findContours(thresh.copy(), cv2.RETR_EXTERNAL,
            cv2.CHAIN_APPROX_SIMPLE)
        cnts = imutils.grab_contours(cnts)
        locs = []

        # loop over the contours
        for (i, c) in enumerate(cnts):
            # compute the bounding box of the contour, then use the
            # bounding box coordinates to derive the aspect ratio
            (x, y, w, h) = cv2.boundingRect(c)
            ar = w / float(h)

            # since credit cards used a fixed size fonts with 4 groups
            # of 4 digits, we can prune potential contours based on the
            # aspect ratio
            if ar > 2.5 and ar < 6.0:
                # contours can further be pruned on minimum/maximum width
                # and height
                if (w > 40 and w < 255) and (h > 10 and h < 60):
                    # append the bounding box region of the digits group
                    # to our locations list
                    locs.append((x, y, w, h))

        # sort the digit locations from left-to-right, then initialize the
        # list of classified digits
        locs = sorted(locs, key=lambda x:x[0])
        output = []

        # loop over the 4 groupings of 4 digits
        for (i, (gX, gY, gW, gH)) in enumerate(locs):
            # initialize the list of group digits
            groupOutput = []

            # extract the group ROI of 4 digits from the grayscale image,
            # then apply thresholding to segment the digits from the
            # background of the credit card
            group = gray[gY - 5:gY + gH + 5, gX - 5:gX + gW + 5]
            group = cv2.threshold(group, 0, 255,
                cv2.THRESH_BINARY_INV | cv2.THRESH_OTSU)[1]
            group = cv2.morphologyEx(group, cv2.MORPH_CLOSE, self.sqKernel)

            if showResult:
                cv2.imshow("Image", group)
                cv2.waitKey(0)
                cv2.destroyAllWindows()

            # detect the contours of each individual digit in the group,
            # then sort the digit contours from left to right
            digitCnts = cv2.findContours(group.copy(), cv2.RETR_EXTERNAL,
                cv2.CHAIN_APPROX_SIMPLE)
            digitCnts = imutils.grab_contours(digitCnts)
            digitCnts = contours.sort_contours(digitCnts,
                method="left-to-right")[0]

            # loop over the digit contours
            for c in digitCnts:
                # compute the bounding box of the individual digit, extract
                # the digit, and resize it to have the same fixed size as
                # the reference OCR-A images
                (x, y, w, h) = cv2.boundingRect(c)                
                roi = group[y:y + h, x:x + w]
                roi = cv2.resize(roi, (57, 88))
                
                if (h < 10):
                    continue

                if showResult:
                    cv2.imshow("Image", roi)
                    cv2.waitKey(0)
                    cv2.destroyAllWindows()

                # initialize a list of template matching scores
                scores = []

                # loop over the reference digit name and digit ROI
                for (digit, digitROI) in self.digits.items():
                    # apply correlation-based template matching, take the
                    # score, and update the scores list
                    result = cv2.matchTemplate(roi, digitROI,
                        cv2.TM_CCOEFF)
                    (_, score, _, _) = cv2.minMaxLoc(result)
                    scores.append(score)

                # the classification for the digit ROI will be the reference
                # digit name with the *largest* template matching score
                groupOutput.append(str(np.argmax(scores)))

            # draw the digit classifications around the group
            cv2.rectangle(image, (gX - 5, gY - 5),
                (gX + gW + 5, gY + gH + 5), (0, 0, 255), 2)
            cv2.putText(image, "".join(groupOutput), (gX, gY - 15),
                cv2.FONT_HERSHEY_SIMPLEX, 0.65, (0, 0, 255), 2)

            # update the output digits list
            output.extend(groupOutput)

        # display the output credit card information to the screen
        output = int(''.join(output))
        print(output)
        #if showResult:
        cv2.imshow("Image", image)
        cv2.waitKey(0)
        cv2.destroyAllWindows()

        return output

def main():
    image = ImageClassification()
    image.readImage(False)

if __name__ == '__main__':
    main()