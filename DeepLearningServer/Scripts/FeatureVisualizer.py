import os
import cv2
import torch
import torch.nn.functional as F
import numpy as np


BATCHSIZE = 128
imagecount = 0

class FeatureVisualizer(object):
    
    def __init__(self, 
        lr=0.2,
        max_factor=1,
        #l2_reg=0.,
        tv_reg=0.005,
        export=False,
        export_path='',
        export_interval=50,
        norm_mean=[0.5487017, 0.5312975, 0.50504637],
        norm_std=[0.1878664, 0.18194826, 0.19830684],
        epochs=200,
        scaleup_schedule={}):
        
        super().__init__()
        self.lr = lr
        self.max_factor = max_factor
        self.tv_reg = tv_reg
        self.export = export
        self.export_interval = export_interval
        self.export_path = export_path
        self.norm_mean = norm_mean
        self.norm_std = norm_std
        self.epochs = epochs
        self.scaleup_schedule = scaleup_schedule
        self.export_transformation = ExportTransform(target_mean=self.norm_mean, target_std=self.norm_std)


    def visualize(self, model, module, device, init_image, n_channels, channels=None):
        global imagecount
        export_meta = []
        if channels is None:
            channels = np.arange(n_channels)

        init_image = init_image.to(device)
        regularize_transformation = Regularizer()
        
        n_batches = int( np.ceil( n_channels / float(BATCHSIZE) ) )
        
        created_image_aggregate = []
        for batchid in range(n_batches):
            channels_batch = channels[batchid * BATCHSIZE : (batchid + 1) * BATCHSIZE]
            n_batch_items = len(channels_batch)
            created_image = init_image.repeat(n_batch_items, 1, 1, 1).detach()
            created_image.requires_grad = True

            for epoch in range(self.epochs):
                with torch.no_grad():
                    created_image = regularize_transformation(created_image)
                    if epoch in self.scaleup_schedule:
                        created_image = F.interpolate(created_image, size=self.scaleup_schedule[epoch], mode='bilinear')
                
                created_image = created_image.detach()
                created_image.requires_grad = True
                model.zero_grad()
                out_dict = model.forward_features({'data' : created_image}, module)
                output = out_dict['activations'][0]['activation']
                loss_max = -torch.stack([output[i, j].mean() for i, j in enumerate(channels_batch)]).sum()
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

                if self.export and (epoch % self.export_interval == 0 or epoch == self.epochs - 1):
                    export_images = self.export_transformation(created_image.detach().cpu())
                    for i, channel in enumerate(channels_batch):
                        path = os.path.join(self.export_path, "_".join([str(channel), str(epoch), str(imagecount) + ".jpg"]))
                        export_meta.append({'path' : path, 'channel' : int(channel), 'epoch' : epoch})
                        cv2.imwrite(path, export_images[i].transpose((1,2,0)))
                        imagecount += 1

            created_image_aggregate.append(created_image.detach().cpu())

        created_image = torch.cat(created_image_aggregate, 0)
        
        return created_image, export_meta


    def total_variation_loss(self, x):
        dh = (x[:,:,1:,:] - x[:,:,:-1,:])**2
        dh = dh.reshape(x.shape[0], x.shape[1], -1)

        dv = (x[:,:,:,1:] - x[:,:,:,:-1])**2
        dv = dv.reshape(x.shape[0], x.shape[1], -1)
        return torch.norm(torch.cat([dh, dv], dim=2), p=1) / (x.shape[2] * x.shape[3])


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

    def __call__(self, x):
        mean = x.mean((1,2,3), keepdims=True)
        std = x.std((1,2,3), keepdims=True)
        x_reg = ((x - mean) / std)
        p = 0.05
        x_new = ((1. - p) * x) +  p * x_reg
        return x_new