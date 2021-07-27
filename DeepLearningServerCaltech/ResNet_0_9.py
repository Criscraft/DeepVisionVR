from collections import OrderedDict 
import torch
from torch import Tensor
import torch.nn as nn
from torch.utils.model_zoo import load_url as load_state_dict_from_url
from typing import Type, Any, Callable, Union, List, Optional
from ActivationTracker import ActivationTracker, TrackerModule


class ResNet_0_9(nn.Module):
    def __init__(self,
        variant='resnet050', 
        n_classes=100, 
        pretrained=False, 
        freeze_features_until='', #exclusive
        no_gradient_required=False,
        enforce_batchnorm_requires_gradient=False,
        n_layers_to_be_removed_from_blocks=[],
        no_classifier=False,
        activation='relu',
        init_mode='kaiming_normal',
        statedict='',
        strict_loading=True):
        super().__init__()

        arg_dict = {
            'pretrained' : pretrained,
            'num_classes' : n_classes,
            'init_mode' : init_mode,
            'activation' : activation,
        }

        if variant == 'resnet018':
            self.embedded_model = resnet18(**arg_dict)
        elif variant == 'resnet034':
            self.embedded_model = resnet34(**arg_dict)
        elif variant == 'resnet050':
            self.embedded_model = resnet50(**arg_dict)
        elif variant == 'resnet101':
            self.embedded_model = resnet101(**arg_dict)
        elif variant == 'resnet152':
            self.embedded_model = resnet152(**arg_dict)
        elif variant == 'resnext050_32x4d':
            self.embedded_model = resnext50_32x4d(**arg_dict)
        elif variant == 'resnext101_32x8d':
            self.embedded_model = resnext101_32x8d(**arg_dict)
        elif variant == 'wide_resnet050_2':
            self.embedded_model = wide_resnet50_2(**arg_dict)
        elif variant == 'wide_resnet101_2':
            self.embedded_model = wide_resnet101_2(**arg_dict)
        else:
            print('select valid model variant')

        if no_classifier:
            self.embedded_model.classifier = nn.Identity()

        module_dict = OrderedDict([
                ('classifier', self.embedded_model.classifier),
                ('layer4', self.embedded_model.layer4),
                ('layer3', self.embedded_model.layer3),
                ('layer2', self.embedded_model.layer2),
                ('layer1', self.embedded_model.layer1),
            ])
        
        if freeze_features_until:
            for param in self.embedded_model.parameters():
                param.requires_grad = False
            
            if freeze_features_until not in module_dict:
                raise ValueError("freeue_features_until does not match any network module")
            
            for key, module in module_dict.items():
                for param in module.parameters():
                    param.requires_grad = True
                if freeze_features_until == key:
                    break

        if n_layers_to_be_removed_from_blocks:
            modules = [
                self.embedded_model.layer1,
                self.embedded_model.layer2,
                self.embedded_model.layer3,
                self.embedded_model.layer4,
            ]
            for n_layers, layer in zip(n_layers_to_be_removed_from_blocks, modules):
                for i in range(n_layers):
                    layer[-i-1] = nn.Identity()

        if statedict:
            pretrained_dict = torch.load(statedict, map_location=torch.device('cpu'))
            missing = self.load_state_dict(pretrained_dict, strict=strict_loading)
            print('Loading weights from statedict. Missing and unexpected keys:')
            print(missing)

        if enforce_batchnorm_requires_gradient:
            for m in self.embedded_model.modules():
                if isinstance(m, nn.BatchNorm2d) or isinstance(m, nn.BatchNorm1d):
                    for param in m.parameters():
                        param.requires_grad = True

        if no_gradient_required:
            for param in self.embedded_model.parameters():
                param.requires_grad = False
                

    def forward(self, batch):
        if isinstance(batch, dict) and 'data' in batch:
            logits = self.embedded_model(batch['data'])
            out = {'logits' : logits}
            return out
        else:
            return self.embedded_model(batch)


    def forward_features(self, batch):
        track_modules = ActivationTracker()

        if isinstance(batch, dict) and 'data' in batch:
            logits, activation_dict = track_modules.collect_stats(self.embedded_model, batch['data'])
            out = {'logits' : logits, 'activations' : activation_dict}
            return out
        else:
            raise NotImplementedError()

            
    def save(self, statedict_name):
        torch.save(self.state_dict(), statedict_name)

        
MODEL_DIR = '/nfshome/linse/NO_INB_BACKUP/ModelZoo'


model_urls = {
    'resnet18': 'https://download.pytorch.org/models/resnet18-5c106cde.pth',
    'resnet34': 'https://download.pytorch.org/models/resnet34-333f7ec4.pth',
    'resnet50': 'https://download.pytorch.org/models/resnet50-19c8e357.pth',
    'resnet101': 'https://download.pytorch.org/models/resnet101-5d3b4d8f.pth',
    'resnet152': 'https://download.pytorch.org/models/resnet152-b121ed2d.pth',
    'resnext50_32x4d': 'https://download.pytorch.org/models/resnext50_32x4d-7cdf4587.pth',
    'resnext101_32x8d': 'https://download.pytorch.org/models/resnext101_32x8d-8ba56ff5.pth',
    'wide_resnet50_2': 'https://download.pytorch.org/models/wide_resnet50_2-95faca4d.pth',
    'wide_resnet101_2': 'https://download.pytorch.org/models/wide_resnet101_2-32ee1156.pth',
}

# global variable to track the position of modules in the computation graph 
TRACKERINDEX = 0


def conv3x3(in_planes: int, out_planes: int, stride: int = 1, groups: int = 1, dilation: int = 1) -> nn.Conv2d:
    """3x3 convolution with padding"""
    return nn.Conv2d(in_planes, out_planes, kernel_size=3, stride=stride,
                     padding=dilation, groups=groups, bias=False, dilation=dilation)


def conv1x1(in_planes: int, out_planes: int, stride: int = 1) -> nn.Conv2d:
    """1x1 convolution"""
    return nn.Conv2d(in_planes, out_planes, kernel_size=1, stride=stride, bias=False)


class BasicBlock(nn.Module):
    expansion: int = 1

    def __init__(
        self,
        inplanes: int,
        planes: int,
        stride: int = 1,
        downsample: Optional[nn.Module] = None,
        groups: int = 1,
        base_width: int = 64,
        dilation: int = 1,
        norm_layer: Optional[Callable[..., nn.Module]] = None,
        activation_layer=nn.ReLU
    ) -> None:
        super(BasicBlock, self).__init__()
        if norm_layer is None:
            norm_layer = nn.BatchNorm2d
        if groups != 1 or base_width != 64:
            raise ValueError('BasicBlock only supports groups=1 and base_width=64')
        if dilation > 1:
            raise NotImplementedError("Dilation > 1 not supported in BasicBlock")
        # Both self.conv1 and self.downsample layers downsample the input when stride != 1
        self.conv1 = conv3x3(inplanes, planes, stride)
        self.bn1 = norm_layer(planes)
        self.relu_1 = activation_layer(inplace=False)
        self.conv2 = conv3x3(planes, planes)
        self.bn2 = norm_layer(planes)
        self.downsample = downsample
        self.relu_2 = activation_layer(inplace=False)
        self.stride = stride
        
        # Create tracker modules
        global TRACKERINDEX
        TRACKERINDEX += 1
        self.marker = TrackerModule((TRACKERINDEX, 0), "BasicBlock", [(TRACKERINDEX-1, 0)], ignore_activation=True)
        TRACKERINDEX += 1
        self.tracker1 = TrackerModule((TRACKERINDEX, 0), "BasicBlock Conv1", [(TRACKERINDEX-1, 0)])
        TRACKERINDEX += 1
        self.tracker2 = TrackerModule((TRACKERINDEX, 0), "BasicBlock Conv2", [(TRACKERINDEX-1, 0)])
        self.tracker3 = TrackerModule((TRACKERINDEX, 1), "Skip", [(TRACKERINDEX-2, 0)])
        TRACKERINDEX += 1
        self.tracker4 = TrackerModule((TRACKERINDEX, 0), "Sum", [(TRACKERINDEX-1, 0), (TRACKERINDEX-1, 1)])

    def forward(self, x: Tensor) -> Tensor:
        identity = x
        _ = self.marker(x)
        out = self.conv1(x)
        out = self.bn1(out)
        out = self.relu_1(out)
        out = self.tracker1(out)

        out = self.conv2(out)
        out = self.bn2(out)
        out = self.tracker2(out)

        if self.downsample is not None:
            identity = self.downsample(x)
        identity = self.tracker3(identity)

        out += identity
        out = self.relu_2(out)
        out = self.tracker4(out)

        return out


class Bottleneck(nn.Module):
    # Bottleneck in torchvision places the stride for downsampling at 3x3 convolution(self.conv2)
    # while original implementation places the stride at the first 1x1 convolution(self.conv1)
    # according to "Deep residual learning for image recognition"https://arxiv.org/abs/1512.03385.
    # This variant is also known as ResNet V1.5 and improves accuracy according to
    # https://ngc.nvidia.com/catalog/model-scripts/nvidia:resnet_50_v1_5_for_pytorch.

    expansion: int = 4

    def __init__(
        self,
        inplanes: int,
        planes: int,
        stride: int = 1,
        downsample: Optional[nn.Module] = None,
        groups: int = 1,
        base_width: int = 64,
        dilation: int = 1,
        norm_layer: Optional[Callable[..., nn.Module]] = None,
        activation_layer=nn.ReLU
    ) -> None:
        super(Bottleneck, self).__init__()
        if norm_layer is None:
            norm_layer = nn.BatchNorm2d
        width = int(planes * (base_width / 64.)) * groups
        # Both self.conv2 and self.downsample layers downsample the input when stride != 1
        self.conv1 = conv1x1(inplanes, width)
        self.bn1 = norm_layer(width)
        self.conv2 = conv3x3(width, width, stride, groups, dilation)
        self.bn2 = norm_layer(width)
        self.conv3 = conv1x1(width, planes * self.expansion)
        self.bn3 = norm_layer(planes * self.expansion)
        self.relu_1 = activation_layer(inplace=False)
        self.relu_2 = activation_layer(inplace=False)
        self.relu_3 = activation_layer(inplace=False)
        self.downsample = downsample
        self.stride = stride

        global TRACKERINDEX
        TRACKERINDEX += 1
        self.marker = TrackerModule((TRACKERINDEX, 0), "Bottleneck", [(TRACKERINDEX-1, 0)], ignore_activation=True)
        TRACKERINDEX += 1
        self.tracker1 = TrackerModule((TRACKERINDEX, 0), "Bottleneck Conv1", [(TRACKERINDEX-1, 0)])
        TRACKERINDEX += 1
        self.tracker2 = TrackerModule((TRACKERINDEX, 0), "Bottleneck Conv2", [(TRACKERINDEX-1, 0)])
        TRACKERINDEX += 1
        self.tracker3 = TrackerModule((TRACKERINDEX, 0), "Bottleneck Conv3", [(TRACKERINDEX-1, 0)])
        self.tracker4 = TrackerModule((TRACKERINDEX, 1), "Skip", [(TRACKERINDEX-3, 0)])
        TRACKERINDEX += 1
        self.tracker5 = TrackerModule((TRACKERINDEX, 0), "Sum", [(TRACKERINDEX-1, 0), (TRACKERINDEX-1, 1)])

    def forward(self, x: Tensor) -> Tensor:
        identity = x

        _ = self.marker(x)
        out = self.conv1(x)
        out = self.bn1(out)
        out = self.relu_1(out)
        out = self.tracker1(out)

        out = self.conv2(out)
        out = self.bn2(out)
        out = self.relu_2(out)
        out = self.tracker2(out)

        out = self.conv3(out)
        out = self.bn3(out)
        out = self.tracker3(out)

        if self.downsample is not None:
            identity = self.downsample(x)
        identity = self.tracker4(identity)

        out += identity
        out = self.relu_3(out)
        out = self.tracker5(out)

        return out


class ResNet(nn.Module):

    def __init__(
        self,
        block: Type[Union[BasicBlock, Bottleneck]],
        layers: List[int],
        num_classes: int = 1000,
        zero_init_residual: bool = False,
        groups: int = 1,
        width_per_group: int = 64,
        replace_stride_with_dilation: Optional[List[bool]] = None,
        norm_layer: Optional[Callable[..., nn.Module]] = None,
        init_mode='kaiming_normal',
        activation='relu',
    ) -> None:
        super().__init__()

        self.ID = 'ResNet'

        if activation == 'relu':
            activation_layer = nn.ReLU
        elif activation == 'leaky_relu':
            activation_layer = nn.LeakyReLU
        self._activation_layer = activation_layer

        if norm_layer is None:
            norm_layer = nn.BatchNorm2d
        self._norm_layer = norm_layer

        self.inplanes = 64
        self.dilation = 1
        if replace_stride_with_dilation is None:
            # each element in the tuple indicates if we should replace
            # the 2x2 stride with a dilated convolution instead
            replace_stride_with_dilation = [False, False, False]
        if len(replace_stride_with_dilation) != 3:
            raise ValueError("replace_stride_with_dilation should be None "
                             "or a 3-element tuple, got {}".format(replace_stride_with_dilation))
        self.groups = groups
        self.base_width = width_per_group
        
        self.conv1 = nn.Conv2d(3, self.inplanes, kernel_size=7, stride=2, padding=3,
                               bias=False)
        self.conv1.ID = self.ID + '_first_layer'
        
        global TRACKERINDEX
        self.tracker1 = TrackerModule((TRACKERINDEX, 0), "input", [])
        self.bn1 = norm_layer(self.inplanes)
        self.relu = self._activation_layer(inplace=False)
        TRACKERINDEX += 1
        self.tracker2 = TrackerModule((TRACKERINDEX, 0), "conv1", [(TRACKERINDEX-1, 0)])
        self.maxpool = nn.MaxPool2d(kernel_size=3, stride=2, padding=1)
        TRACKERINDEX += 1
        self.tracker3 = TrackerModule((TRACKERINDEX, 0), "maxpool", [(TRACKERINDEX-1, 0)])
        self.layer1 = self._make_layer(block, 64, layers[0])
        self.layer2 = self._make_layer(block, 128, layers[1], stride=2,
                                       dilate=replace_stride_with_dilation[0])
        self.layer3 = self._make_layer(block, 256, layers[2], stride=2,
                                       dilate=replace_stride_with_dilation[1])
        self.layer4 = self._make_layer(block, 512, layers[3], stride=2,
                                       dilate=replace_stride_with_dilation[2])
        TRACKERINDEX += 1
        self.marker = TrackerModule((TRACKERINDEX, 0), "Classification Part", [(TRACKERINDEX-1, 0)], ignore_activation=True)
        self.avgpool = nn.AdaptiveAvgPool2d((1, 1))
        TRACKERINDEX += 1
        self.tracker4 = TrackerModule((TRACKERINDEX, 0), "avgpool")
        self.classifier = nn.Linear(512 * block.expansion, num_classes)
        TRACKERINDEX += 1
        self.tracker5 = TrackerModule((TRACKERINDEX, 0), "output")

        for m in self.modules():
            if isinstance(m, nn.Conv2d):
                if init_mode == 'kaiming_normal':
                    nn.init.kaiming_normal_(m.weight, mode='fan_out', nonlinearity=activation)
                elif init_mode == 'kaiming_uniform':
                    nn.init.kaiming_uniform_(m.weight, mode='fan_out', nonlinearity=activation)
                elif init_mode == 'sparse':
                    nn.init.sparse_(m.weight, sparsity=0.1, std=0.01)
                elif init_mode == 'orthogonal':
                    nn.init.orthogonal_(m.weight, gain=1)
            elif isinstance(m, (nn.BatchNorm2d, nn.GroupNorm)):
                nn.init.constant_(m.weight, 1)
                nn.init.constant_(m.bias, 0)

        # Zero-initialize the last BN in each residual branch,
        # so that the residual branch starts with zeros, and each residual block behaves like an identity.
        # This improves the model by 0.2~0.3% according to https://arxiv.org/abs/1706.02677
        if zero_init_residual:
            for m in self.modules():
                if isinstance(m, Bottleneck):
                    nn.init.constant_(m.bn3.weight, 0)  # type: ignore[arg-type]
                elif isinstance(m, BasicBlock):
                    nn.init.constant_(m.bn2.weight, 0)  # type: ignore[arg-type]

    def _make_layer(self, block: Type[Union[BasicBlock, Bottleneck]], planes: int, blocks: int,
                    stride: int = 1, dilate: bool = False) -> nn.Sequential:
        norm_layer = self._norm_layer
        downsample = None
        activation_layer = self._activation_layer
        previous_dilation = self.dilation

        if dilate:
            self.dilation *= stride
            stride = 1
        if stride != 1 or self.inplanes != planes * block.expansion:
            downsample = nn.Sequential(
                conv1x1(self.inplanes, planes * block.expansion, stride),
                norm_layer(planes * block.expansion),
            )

        layers = []
        layers.append(block(self.inplanes, planes, stride, downsample, self.groups,
                            self.base_width, previous_dilation, norm_layer, activation_layer))
        self.inplanes = planes * block.expansion
        for _ in range(1, blocks):
            layers.append(block(self.inplanes, planes, groups=self.groups,
                                base_width=self.base_width, dilation=self.dilation,
                                norm_layer=norm_layer, activation_layer=activation_layer))

        return nn.Sequential(*layers)

    def _forward_impl(self, x: Tensor) -> Tensor:
        # See note [TorchScript super()]
        x = self.tracker1(x)
        x = self.conv1(x)
        x = self.bn1(x)
        x = self.relu(x)
        x = self.tracker2(x)
        x = self.maxpool(x)
        x = self.tracker3(x)

        x = self.layer1(x)
        x = self.layer2(x)
        x = self.layer3(x)
        x = self.layer4(x)

        x = self.avgpool(x)
        x = torch.flatten(x, 1)
        _ = self.marker(x)
        x = self.tracker4(x)
        x = self.classifier(x)
        x = self.tracker5(x)

        return x

    def forward(self, x: Tensor) -> Tensor:
        return self._forward_impl(x)


def _resnet(
    arch: str,
    block: Type[Union[BasicBlock, Bottleneck]],
    layers: List[int],
    pretrained: bool,
    progress: bool,
    **kwargs: Any
) -> ResNet:
    model = ResNet(block, layers, **kwargs)
    if pretrained:
        state_dict = load_state_dict_from_url(model_urls[arch], progress=progress, model_dir=MODEL_DIR)
        model.load_state_dict(state_dict, strict=False)
    return model


def resnet18(pretrained: bool = False, progress: bool = True, **kwargs: Any) -> ResNet:
    r"""ResNet-18 model from
    `"Deep Residual Learning for Image Recognition" <https://arxiv.org/pdf/1512.03385.pdf>`_.

    Args:
        pretrained (bool): If True, returns a model pre-trained on ImageNet
        progress (bool): If True, displays a progress bar of the download to stderr
    """
    return _resnet('resnet18', BasicBlock, [2, 2, 2, 2], pretrained, progress,
                   **kwargs)



def resnet34(pretrained: bool = False, progress: bool = True, **kwargs: Any) -> ResNet:
    r"""ResNet-34 model from
    `"Deep Residual Learning for Image Recognition" <https://arxiv.org/pdf/1512.03385.pdf>`_.

    Args:
        pretrained (bool): If True, returns a model pre-trained on ImageNet
        progress (bool): If True, displays a progress bar of the download to stderr
    """
    return _resnet('resnet34', BasicBlock, [3, 4, 6, 3], pretrained, progress,
                   **kwargs)



def resnet50(pretrained: bool = False, progress: bool = True, **kwargs: Any) -> ResNet:
    r"""ResNet-50 model from
    `"Deep Residual Learning for Image Recognition" <https://arxiv.org/pdf/1512.03385.pdf>`_.

    Args:
        pretrained (bool): If True, returns a model pre-trained on ImageNet
        progress (bool): If True, displays a progress bar of the download to stderr
    """
    return _resnet('resnet50', Bottleneck, [3, 4, 6, 3], pretrained, progress,
                   **kwargs)



def resnet101(pretrained: bool = False, progress: bool = True, **kwargs: Any) -> ResNet:
    r"""ResNet-101 model from
    `"Deep Residual Learning for Image Recognition" <https://arxiv.org/pdf/1512.03385.pdf>`_.

    Args:
        pretrained (bool): If True, returns a model pre-trained on ImageNet
        progress (bool): If True, displays a progress bar of the download to stderr
    """
    return _resnet('resnet101', Bottleneck, [3, 4, 23, 3], pretrained, progress,
                   **kwargs)



def resnet152(pretrained: bool = False, progress: bool = True, **kwargs: Any) -> ResNet:
    r"""ResNet-152 model from
    `"Deep Residual Learning for Image Recognition" <https://arxiv.org/pdf/1512.03385.pdf>`_.

    Args:
        pretrained (bool): If True, returns a model pre-trained on ImageNet
        progress (bool): If True, displays a progress bar of the download to stderr
    """
    return _resnet('resnet152', Bottleneck, [3, 8, 36, 3], pretrained, progress,
                   **kwargs)



def resnext50_32x4d(pretrained: bool = False, progress: bool = True, **kwargs: Any) -> ResNet:
    r"""ResNeXt-50 32x4d model from
    `"Aggregated Residual Transformation for Deep Neural Networks" <https://arxiv.org/pdf/1611.05431.pdf>`_.

    Args:
        pretrained (bool): If True, returns a model pre-trained on ImageNet
        progress (bool): If True, displays a progress bar of the download to stderr
    """
    kwargs['groups'] = 32
    kwargs['width_per_group'] = 4
    return _resnet('resnext50_32x4d', Bottleneck, [3, 4, 6, 3],
                   pretrained, progress, **kwargs)



def resnext101_32x8d(pretrained: bool = False, progress: bool = True, **kwargs: Any) -> ResNet:
    r"""ResNeXt-101 32x8d model from
    `"Aggregated Residual Transformation for Deep Neural Networks" <https://arxiv.org/pdf/1611.05431.pdf>`_.

    Args:
        pretrained (bool): If True, returns a model pre-trained on ImageNet
        progress (bool): If True, displays a progress bar of the download to stderr
    """
    kwargs['groups'] = 32
    kwargs['width_per_group'] = 8
    return _resnet('resnext101_32x8d', Bottleneck, [3, 4, 23, 3],
                   pretrained, progress, **kwargs)



def wide_resnet50_2(pretrained: bool = False, progress: bool = True, **kwargs: Any) -> ResNet:
    r"""Wide ResNet-50-2 model from
    `"Wide Residual Networks" <https://arxiv.org/pdf/1605.07146.pdf>`_.

    The model is the same as ResNet except for the bottleneck number of channels
    which is twice larger in every block. The number of channels in outer 1x1
    convolutions is the same, e.g. last block in ResNet-50 has 2048-512-2048
    channels, and in Wide ResNet-50-2 has 2048-1024-2048.

    Args:
        pretrained (bool): If True, returns a model pre-trained on ImageNet
        progress (bool): If True, displays a progress bar of the download to stderr
    """
    kwargs['width_per_group'] = 64 * 2
    return _resnet('wide_resnet50_2', Bottleneck, [3, 4, 6, 3],
                   pretrained, progress, **kwargs)



def wide_resnet101_2(pretrained: bool = False, progress: bool = True, **kwargs: Any) -> ResNet:
    r"""Wide ResNet-101-2 model from
    `"Wide Residual Networks" <https://arxiv.org/pdf/1605.07146.pdf>`_.

    The model is the same as ResNet except for the bottleneck number of channels
    which is twice larger in every block. The number of channels in outer 1x1
    convolutions is the same, e.g. last block in ResNet-50 has 2048-512-2048
    channels, and in Wide ResNet-50-2 has 2048-1024-2048.

    Args:
        pretrained (bool): If True, returns a model pre-trained on ImageNet
        progress (bool): If True, displays a progress bar of the download to stderr
    """
    kwargs['width_per_group'] = 64 * 2
    return _resnet('wide_resnet101_2', Bottleneck, [3, 4, 23, 3],
                   pretrained, progress, **kwargs)