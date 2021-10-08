import os
import cv2
import torch
import torch.nn as nn
import torch.nn.functional as F 
from torchvision import transforms
import torchgeometry as tgm
import numpy as np
import random


BATCHSIZE = 64
imagecount = 0


class FeatureVisualizer(object):
    
    def __init__(self,
        max_factor = 1,
        export = False,
        export_path = '',
        export_interval=50,
        target_size = (150, 175), 
        norm_mean = [0.5487017, 0.5312975, 0.50504637],
        norm_std = [0.1878664, 0.18194826, 0.19830684],
        epochs = 200,
        lr=0.15,
        distribution_reg_blend=0.05,
        scale = 0.05,
        degrees = 10, 
        blur_sigma = 0.5,
        roll = 5,
        epochs_without_robustness = 5):
        
        super().__init__()
        self.lr = lr
        self.max_factor = max_factor
        self.export = export
        self.export_interval = export_interval
        self.export_path = export_path
        self.epochs = epochs
        self.regularize_transformation = Regularizer(norm_mean, distribution_reg_blend, target_size, scale, degrees, blur_sigma, roll)
        self.export_transformation = ExportTransform(target_mean=norm_mean, target_std=norm_std)
        self.epochs_without_robustness = epochs_without_robustness


    def visualize(self, model, module, device, init_image, n_channels, channels=None):
        global imagecount
        export_meta = []
        if channels is None:
            channels = np.arange(n_channels)

        init_image = init_image.to(device)

        if torch.all(init_image[0] == init_image[1]) and torch.all(init_image[0] == init_image[2]):
            grayscale = True
        else:
            grayscale = False
        
        n_batches = int( np.ceil( n_channels / float(BATCHSIZE) ) )
        
        created_image_aggregate = []
        for batchid in range(n_batches):
            channels_batch = channels[batchid * BATCHSIZE : (batchid + 1) * BATCHSIZE]
            n_batch_items = len(channels_batch)
            created_image = init_image.repeat(n_batch_items, 1, 1, 1).detach()

            for epoch in range(self.epochs):
                if epoch < self.epochs - self.epochs_without_robustness:
                    with torch.no_grad():
                        created_image = self.regularize_transformation(created_image)
                
                created_image = created_image.detach().clone()
                created_image.requires_grad = True
                if hasattr(created_image, 'grad') and created_image.grad is not None:
                    created_image.grad.data.zero_()
                model.zero_grad()

                out_dict = model.forward_features({'data' : created_image}, module)
                output = out_dict['activations'][0]['activation']
                loss_max = -torch.stack([output[i, j].mean() for i, j in enumerate(channels_batch)]).sum()

                loss = self.max_factor * loss_max
                if torch.isnan(loss):
                    print("loss is none, reset image")
                    created_image = init_image.repeat(n_batch_items, 1, 1, 1).detach()
                    continue

                loss.backward()

                gradients = created_image.grad / (torch.sqrt((created_image.grad**2).mean()) + 1e-6)
                created_image = created_image - gradients * self.lr
                if grayscale:
                    created_image = created_image.mean(1, keepdims=True)
                    created_image = created_image.expand(-1, 3, -1, -1)


                if epoch % 10 == 0:
                    print(epoch, loss_max.item())

                if self.export and (epoch % self.export_interval == 0 or epoch == self.epochs - 1):
                    with torch.no_grad():
                        export_images = self.export_transformation(created_image.detach().cpu())
                        for i, channel in enumerate(channels_batch):
                            path = os.path.join(self.export_path, "_".join([str(channel), str(epoch), str(imagecount) + ".jpg"]))
                            export_meta.append({'path' : path, 'channel' : int(channel), 'epoch' : epoch})
                            cv2.imwrite(path, export_images[i].transpose((1,2,0)))
                            imagecount += 1

            created_image_aggregate.append(created_image.detach().cpu())

        created_image = torch.cat(created_image_aggregate, 0)
        
        return created_image, export_meta


class ExportTransform(object):
    def __init__(self, target_mean, target_std=None):
        self.target_mean = np.array(target_mean).reshape((1,3,1,1))
        self.target_std = np.array(target_std).reshape((1,3,1,1))

    def __call__(self, x):
        if isinstance(x, torch.Tensor):
            x = x.cpu().numpy()

        x = x * self.target_std
        x = x + self.target_mean
        x = x.clip(0., 1.) * 255.9
        x = x.astype(np.uint8)
        return x


class Regularizer(object):

    def __init__(self, norm_mean, distribution_reg_blend, target_size, scale, degrees, blur_sigma, roll):
        if scale > 0.:
            scale = (1., 1. + scale)
        else:
            scale = None

        if blur_sigma > 0.:
            blurring = tgm.image.GaussianBlur((5, 5), (blur_sigma, blur_sigma))# transforms.GaussianBlur(5, sigma=(0.1, blur_sigma))
        else:
            blurring = Identity()

        self.transformation = transforms.Compose([
            transforms.Pad(roll*2, padding_mode='edge'),
            transforms.RandomApply(torch.nn.ModuleList([
                transforms.RandomRotation(degrees=degrees),
                ]), p=0.3),
            transforms.RandomApply(torch.nn.ModuleList([
                transforms.RandomAffine(degrees=0, scale=scale, interpolation=transforms.InterpolationMode.BILINEAR),
                ]), p=0.05),
            blurring,
            transforms.CenterCrop(target_size),
            transforms.RandomApply(torch.nn.ModuleList([
                RandomRoll(roll),
                ]), p=0.3),
            DistributionRegularizer(distribution_reg_blend)
        ])

    def __call__(self, x):
        return self.transformation(x)


class DistributionRegularizer(nn.Module):

    def __init__(self, blend):
        super().__init__()
        self.blend = blend

    def forward(self, x):
        mean = x.mean((1,2,3), keepdims=True)
        std = x.std((1,2,3), keepdims=True)
        x_reg = ((x - mean) / std)
        x_new = ((1. - self.blend) * x) +  self.blend * x_reg
        return x_new


class RandomRoll(nn.Module):

    def __init__(self, roll):
        super().__init__()
        self.roll = roll

    def forward(self, x):
        x = torch.roll(x, (random.randint(-self.roll, self.roll), random.randint(-self.roll, self.roll)), dims=(2, 3))
        return x


class Identity(nn.Module):

    def __init__(self):
        super().__init__()

    def forward(self, x):
        return x