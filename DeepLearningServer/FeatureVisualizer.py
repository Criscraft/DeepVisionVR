import os
import cv2
import torch
import torch.nn.functional as F 
from torchvision import transforms
import numpy as np

class FeatureVisualizer(object):
    
    def __init__(self, 
        lr=0.1,
        max_factor = 1,
        #l2_reg=0.,
        tv_reg=0.005,
        export = False,
        export_interval=10,
        init_img_shape=(80, 80),
        norm_mean = [0.5487017, 0.5312975, 0.50504637],
        norm_std = [0.1878664, 0.18194826, 0.19830684],
        epochs = 200,
        scaleup_schedule = {}):
        
        super().__init__()
        self.lr = lr
        self.max_factor = max_factor
        #self.l2_reg = l2_reg
        self.tv_reg = tv_reg
        self.export = export
        self.export_interval = export_interval
        self.init_img_shape = init_img_shape
        self.norm_mean = norm_mean
        self.norm_std = norm_std
        self.epochs = epochs
        self.scaleup_schedule = scaleup_schedule


    def generate_noise_image(self, device):
        return torch.randn((3, self.init_img_shape[0], self.init_img_shape[1]), device=device)


    def visualize(self, model, module, device, n_channels, init_image):
        init_image = init_image.to(device)
        regularize_transformation = Regularizer()
        export_transformation = ToImage(target_mean=self.norm_mean, target_std=self.norm_std)
        created_image = init_image.repeat(n_channels, 1, 1, 1)
        created_image.requires_grad = True

        for epoch in range(self.epochs):
            with torch.no_grad():
                created_image = regularize_transformation(created_image)
                if epoch in self.scaleup_schedule:
                    created_image = F.interpolate(created_image, size=self.scaleup_schedule[epoch], mode='bilinear')
            
            created_image = created_image.detach()
            created_image.requires_grad = True
            out_dict = model.forward_features({'data' : created_image}, module)
            output = out_dict['activations'][0]['activation']
            loss_max = -torch.stack([output[i, i].mean() for i in range(n_channels)]).sum()
            #loss_l2_reg = torch.norm(created_image, p=2) / (torch.tensor(np.sqrt(created_image.shape[1] * created_image.shape[2] * created_image.shape[3]), device=device))
            #loss_l2_reg = (created_image**2).mean()
            loss_tv_re = self.total_variation_loss(created_image)
            #loss = self.max_factor * loss_max + self.l2_reg * loss_l2_reg + self.tv_reg * loss_tv_re
            loss = self.max_factor * loss_max + self.tv_reg * loss_tv_re
            
            loss.backward()
            gradients = created_image.grad / (torch.sqrt((created_image.grad**2).mean()) + 1e-6)
            created_image = created_image - gradients * self.lr

            #print(epoch, loss_max.item(), loss_l2_reg.item(), loss_tv_re.item())
            print(epoch, loss_max.item(), loss_tv_re.item())

            if self.export and epoch % self.export_interval == 0:
                with torch.no_grad():
                    export_image = export_transformation(created_image.detach().cpu())[0]
                path = os.path.join("tmp_images", '{:d}.jpg'.format(epoch))
                cv2.imwrite(path, export_image.transpose((1,2,0)))

        
        with torch.no_grad():
            export_image = export_transformation(created_image.detach().cpu())
        
        if self.export:
            path = os.path.join("tmp_images", '{:d}.jpg'.format(epoch))
            cv2.imwrite(path, export_image[0].transpose((1,2,0)))

        return export_image, created_image.detach()


    def total_variation_loss(self, x):
        dh = (x[:,:,1:,:] - x[:,:,:-1,:])**2
        dh = dh.reshape(x.shape[0], x.shape[1], -1)

        dv = (x[:,:,:,1:] - x[:,:,:,:-1])**2
        dv = dv.reshape(x.shape[0], x.shape[1], -1)
        return torch.norm(torch.cat([dh, dv], dim=2), p=1) / (x.shape[2] * x.shape[3])


class ToImage(object):
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

    def __call__(self, x):
        mean = x.mean((1,2,3), keepdims=True)
        std = x.std((1,2,3), keepdims=True)
        x_reg = ((x - mean) / std)
        p = 0.05
        x_new = ((1. - p) * x) +  p * x_reg
        return x_new