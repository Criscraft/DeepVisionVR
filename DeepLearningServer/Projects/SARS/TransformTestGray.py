from torchvision import transforms
import numpy as np
import numpy.random as random


class PadToSize(object):
    """
    Adds padding in order to reach a desired image size
    """

    def __init__(self, image_size, fill):
        self.image_size = image_size
        self.fill = fill

    @staticmethod
    def get_params(img, image_size):
        """Get parameters for ``crop`` for a random crop.
        """
        w, h = img.size
        delta_height = image_size[0] - h
        delta_width = image_size[1] - w
        p_left = random.randint(0,delta_width+1)
        p_right = delta_width - p_left
        p_top = random.randint(0,delta_height+1)
        p_bottom = delta_height - p_top
        padding = (p_left, p_top, p_right, p_bottom)
        return padding

    def __call__(self, img):
        """
        Args:
            img (PIL Image): Image to be cropped.

        Returns:
            PIL Image: Cropped image.
        """
        padding = self.get_params(img, self.image_size)
        return transforms.functional.pad(img, padding, fill=self.fill)

    def __repr__(self):
        return self.__class__.__name__ + '(crop_ratio={0})'.format(self.image_size)


class RandomScaleWithMaxSize(object):
    """
    Adds padding in order to reach a desired image size
    """

    def __init__(self, max_size, min_coverage):
        self.max_size = max_size
        self.min_coverage = min_coverage

    @staticmethod
    def get_params(img, max_size, min_coverage):
        """Get parameters for ``crop`` for a random crop.
        """
        w, h = img.size
        img_aspect_ratio = h / w
        allowed_aspect_ratio = max_size[0] / max_size[1]

        if img_aspect_ratio >= allowed_aspect_ratio:
            # h is comparably large and is the axis to control scaling
            max_scalefactor = max_size[0] / h
        else:
            # w is comparably large and is the axis to control scaling
            max_scalefactor = max_size[1] / w
        
        min_scalefactor = np.sqrt(min_coverage*max_size[0]*max_size[1]/h/w)

        if min_scalefactor > max_scalefactor:
            # img has an extreme aspect ratio and the min_coverage constraint cannot be fulfilled
            scalefactor = max_scalefactor
        else:
            scalefactor = min_scalefactor + random.random() * (max_scalefactor - min_scalefactor)
        
        h_new = int(h*scalefactor)
        w_new = int(w*scalefactor)

        return (h_new, w_new)

    def __call__(self, img):
        """
        Args:
            img (PIL Image): Image to be cropped.

        Returns:
            PIL Image: Cropped image.
        """
        desired_size = self.get_params(img, self.max_size, self.min_coverage)
        return transforms.functional.resize(img, desired_size)

    def __repr__(self):
        return self.__class__.__name__ + '(max_size={0}, min_coverage={1})'.format(self.max_size, self.min_coverage)


class TransformTestGray(object):

    def __init__(self,
        img_shape = (128, 128), 
        norm_mean=[x / 255.0 for x in [125.3, 123.0, 113.9]],
        norm_std=[x / 255.0 for x in [63.0, 62.1, 66.7]]):
        super().__init__()

        mean_pil = tuple([int(x*255) for x in norm_mean])

        self.transform = transforms.Compose([
            RandomScaleWithMaxSize(img_shape, 0.8),
            PadToSize(img_shape, mean_pil),
            transforms.Grayscale(3),
            transforms.ToTensor(),
            transforms.Normalize(mean=norm_mean, std=norm_std),
        ])


    def __call__(self, img):
        return self.transform(img)