from torchvision import transforms


class TransformToTensor(object):

    def __init__(self): 
        super().__init__()

        self.transform = transforms.Compose([
            transforms.ToTensor(),
        ])


    def __call__(self, img):
        return self.transform(img)