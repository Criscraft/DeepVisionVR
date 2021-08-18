import os
import cv2
import torch
import torch.nn.functional as F 
from torchvision import transforms
import numpy as np

class FeatureVisualizer(object):
    
    def __init__(self, 
        lr=0.001, 
        l2_reg=0., 
        tv_reg=0., 
        export_interval=1, 
        img_shape=(150, 175),
        norm_mean = [1., 1., 1.],
        norm_std = [0., 0., 0.]):
        
        super().__init__()
        self.lr = lr
        self.l2_reg = l2_reg
        self.tv_reg = tv_reg
        self.export_interval = export_interval
        self.img_shape = img_shape
        self.norm_mean = norm_mean
        self.norm_std = norm_std
        self.scaleup_schedule = {}


    def visualize(self, model, module):
        channel_id = 0
        regularize_transformation = transforms.Lambda(lambda x : x)
        export_transformation = ToImage(norm_mean=self.norm_mean, norm_std=self.norm_std)
        created_image = torch.rand((1, 3, self.img_shape[0], self.img_shape[1]), device=self.device)
        created_image.requires_grad = True
        optimizer = torch.optim.SGD([created_image], lr=0.0001, momentum=0.9, weight_decay=0)

        for epoch in range(1):
            with torch.no_grad():
                created_image = regularize_transformation(created_image)
                if epoch in self.scaleup_schedule:
                    created_image = F.interpolate(created_image, size=self.scaleup_schedule[epoch], mode='trilinear')

            out_dict = self.model.forward_features({'data' : created_image}, module)
            print(out_dict)
            output = out_dict['activations']['activation'][0, channel_id, ...]
            loss = -output.mean() 
            loss += self.l2_reg * torch.norm((created_image - 0.5), p=2) / torch.sqrt(created_image.shape[1] * created_image.shape[2] * created_image.shape[3])
            loss += self.tv_reg * self.total_variation_loss(created_image)
            created_image.zero_grad()
            loss.backward()
            optimizer.step()

            if epoch % self.export_interval == 0:
                with torch.no_grad():
                    export_image = export_transformation(created_image.detach().cpu())
                path = os.path.join("tmp_images", '{:d}.jpg'.format(epoch))
                cv2.imwrite(path, export_image)

        with torch.no_grad():
            export_image = export_transformation(created_image.detach().cpu())
        path = os.path.join("tmp_images", '{:d}.jpg'.format(epoch))
        cv2.imwrite(path, export_image)


    def total_variation_loss(self, x):
        dh = (x[:,:,1:,:] - x[:,:,:-1,:])**2
        dh = dh.reshape(x.shape[0], x.shape[1], -1)

        dv = (x[:,:,:,1:] - x[:,:,:,:-1])**2
        dv = dv.reshape(x.shape[0], x.shape[1], -1)
        return torch.norm(torch.cat([dh, dv], dim=2), p=1) / (x.shape[2] * x.shape[3])


class ToImage(object):
    def __init__(self, target_mean, target_std=None):
        self.target_mean = np.array(target_mean).reshape((3,1,1))
        self.target_std = np.array(target_std).reshape((3,1,1))

    def __call__(self, x):
        if isinstance(x, torch.Tensor):
            x = x.cpu().numpy()

        x = x * self.target_std
        x = x + self.target_mean

        x = x.clip(0., 1.) * 255.9
        x = x.astype(np.uint8)
        return x


class ScaleUp(object):

    def __init__(self, interval=2, target_size=(500,500), rate_per_epoch=1):
        self.interval = interval
        self.epoch = 1
        self.target_size = np.array(target_size)
        self.rate_per_epoch = rate_per_epoch
        self.initial_size = [0, 0]

    def __call__(self, tensor):
        if self.epoch == 1:
            self.initial_size = np.array(tensor.shape[1:])
        
        self.epoch = self.epoch + 1
        return tensor