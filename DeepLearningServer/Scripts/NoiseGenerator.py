import torch

class NoiseGenerator:

    def __init__(self, device, shape, grayscale=False):
        self.device = device
        self.active_noise_image = None
        self.shape = shape
        self.grayscale = grayscale


    def generate_noise_image(self):
        if self.grayscale:
            self.active_noise_image = torch.randn((1, self.shape[0], self.shape[1]), device=self.device)
            self.active_noise_image = self.active_noise_image.repeat(3, 1, 1)
        else:
            self.active_noise_image = torch.randn((3, self.shape[0], self.shape[1]), device=self.device)


    def get_noise_image(self):
        if self.active_noise_image is None:
            self.generate_noise_image()
        return self.active_noise_image