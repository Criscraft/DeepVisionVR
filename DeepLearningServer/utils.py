import torch
import numpy as np
import base64
import cv2


class TransformToUint(object):

    def __init__(self):
        self.v_max = 0
        self.v_min = 0

    def __call__(self, data, fitting):
        if isinstance(data, torch.Tensor):
            data = data.cpu().numpy()
        if fitting:
            self.v_max = data.max()
            self.v_min = data.min()
            #self.v_max = np.percentile(out, 98)
            #self.v_min = np.percentile(out, 2)

        # boost dark regions
        stretch_constant = 10
        data = (data - self.v_min) / (self.v_max - self.v_min + 1e-6) * stretch_constant
        data = np.log(data + 1) / np.log(stretch_constant + 1) * 255
        
        data = data.clip(0, 255)
        data = data.astype("uint8")
        return data
        

def tensor_to_string(tensor):
    # tensor has shape (C, H, W) with channels RGB
    image = tensor.detach().cpu().numpy()
    image = image * 255
    image = image.astype("uint8")
    image = image[np.array([2,1,0])] # for sorting color channels to a unity friendly format BGR
    image = image.transpose([1,2,0]) # put channel dimension to last, new shape (H, W, C)
    image_enc = encode_image(image)
    return image_enc


def encode_image(data):
    _, buffer = cv2.imencode('.png', data)
    return base64.b64encode(buffer).decode('utf-8')