import re
import torch
import torch.nn as nn
import torch.nn.functional as F
import torch.utils.checkpoint as cp
from collections import OrderedDict
from torch.utils.model_zoo import load_url as load_state_dict_from_url
from torch import Tensor
from torch.jit.annotations import List
from ActivationTracker import ActivationTracker, TrackerModule

class CovidDenseNet(nn.Module):
    def __init__(self, 
        variant='densenet121', 
        n_classes=100, 
        pretrained=False, 
        freeze_features_until='', #inclusive
        blocks=[6, 10, 2, 1],
        num_transition_features=[128, 256, 512],
        no_classifier=False,
        statedict='',
        strict_loading=True):
        super().__init__()
        
        arg_dict = {
            'num_classes' : n_classes,
            'num_transition_features' : num_transition_features,
        }

        if variant == 'densenet121':
            self.embedded_model = _densenet('densenet121', 32, blocks, 64, pretrained, False, **arg_dict)
        else:
            print('select valid model variant')
        
        if no_classifier:
            self.embedded_model.classifier = nn.Identity()
            
        if statedict:
            pretrained_dict = torch.load(statedict, map_location=torch.device('cpu'))
            missing = self.load_state_dict(pretrained_dict, strict=strict_loading)
            print('Loading weights from statedict. Missing and unexpected keys:')
            print(missing)
        
        feature_module_key_list = ['denseblock1', 'transition1', 'denseblock2', 'transition2', 'denseblock3', 'transition3', 'denseblock4', 'norm5']
        feature_module_list = [self.embedded_model.features._modules[key] for key in feature_module_key_list]
        feature_module_key_list.append('classifier')
        feature_module_list.append(self.embedded_model.classifier)
        feature_module_key_list.reverse()
        feature_module_list.reverse()
        module_dict = OrderedDict(zip(feature_module_key_list, feature_module_list))
        
        if freeze_features_until:
            for param in self.embedded_model.parameters():
                param.requires_grad = False
            
            if freeze_features_until not in module_dict:
                raise ValueError("freeze_features_until does not match any network module")
            
            for key, module in module_dict.items():
                if freeze_features_until == key:
                    break
                for param in module.parameters():
                    param.requires_grad = True
                

    def forward(self, batch):
        if isinstance(batch, dict) and 'data' in batch:
            return {'logits' : self.embedded_model(batch['data'])}
        else:
            return self.embedded_model(batch)


    def forward_features(self, batch, module=None):
        track_modules = ActivationTracker()

        if isinstance(batch, dict) and 'data' in batch:
            logits, activation_dict = track_modules.collect_stats(self.embedded_model, batch['data'], module)
            out = {'logits' : logits, 'activations' : activation_dict}
            return out
        else:
            raise NotImplementedError()
 
            
    def save(self, statedict_name):
        torch.save(self.state_dict(), statedict_name)


model_urls = {
    'densenet121': 'https://download.pytorch.org/models/densenet121-a639ec97.pth',
    'densenet169': 'https://download.pytorch.org/models/densenet169-b2777c0a.pth',
    'densenet201': 'https://download.pytorch.org/models/densenet201-c1103571.pth',
    'densenet161': 'https://download.pytorch.org/models/densenet161-8d451a50.pth',
}

TRACKERINDEX = 0


class _DenseLayer(nn.Module):
    def __init__(self, num_input_features, growth_rate, bn_size, drop_rate, memory_efficient=False):
        super(_DenseLayer, self).__init__()
        self.add_module('norm1', nn.BatchNorm2d(num_input_features)),
        self.add_module('relu1', nn.ReLU(inplace=True)),
        self.add_module('conv1', nn.Conv2d(num_input_features, bn_size *
                                           growth_rate, kernel_size=1, stride=1,
                                           bias=False)),
        self.add_module('norm2', nn.BatchNorm2d(bn_size * growth_rate)),
        self.add_module('relu2', nn.ReLU(inplace=True)),
        self.add_module('conv2', nn.Conv2d(bn_size * growth_rate, growth_rate,
                                           kernel_size=3, stride=1, padding=1,
                                           bias=False)),
        self.drop_rate = float(drop_rate)
        self.memory_efficient = memory_efficient

    def bn_function(self, inputs):
        # type: (List[Tensor]) -> Tensor
        concated_features = torch.cat(inputs, 1)
        bottleneck_output = self.conv1(self.relu1(self.norm1(concated_features)))  # noqa: T484
        return bottleneck_output

    # todo: rewrite when torchscript supports any
    def any_requires_grad(self, input):
        # type: (List[Tensor]) -> bool
        for tensor in input:
            if tensor.requires_grad:
                return True
        return False

    @torch.jit.unused  # noqa: T484
    def call_checkpoint_bottleneck(self, input):
        # type: (List[Tensor]) -> Tensor
        def closure(*inputs):
            return self.bn_function(*inputs)

        return cp.checkpoint(closure, input)

    @torch.jit._overload_method  # noqa: F811
    def forward(self, input):
        # type: (List[Tensor]) -> (Tensor)
        pass

    @torch.jit._overload_method  # noqa: F811
    def forward(self, input):
        # type: (Tensor) -> (Tensor)
        pass

    # torchscript does not yet support *args, so we overload method
    # allowing it to take either a List[Tensor] or single Tensor
    def forward(self, input):  # noqa: F811
        if isinstance(input, Tensor):
            prev_features = [input]
        else:
            prev_features = input

        if self.memory_efficient and self.any_requires_grad(prev_features):
            if torch.jit.is_scripting():
                raise Exception("Memory Efficient not supported in JIT")

            bottleneck_output = self.call_checkpoint_bottleneck(prev_features)
        else:
            bottleneck_output = self.bn_function(prev_features)

        new_features = self.conv2(self.relu2(self.norm2(bottleneck_output)))
        if self.drop_rate > 0:
            new_features = F.dropout(new_features, p=self.drop_rate,
                                     training=self.training)
        return new_features


class _DenseBlock(nn.ModuleDict):
    _version = 2

    def __init__(self, num_layers, num_input_features, bn_size, growth_rate, drop_rate, memory_efficient=False):
        super(_DenseBlock, self).__init__()

        global TRACKERINDEX
        TRACKERINDEX += 1
        tracker_second_index = -1
        tracker_base_index = TRACKERINDEX
        marker = TrackerModule((TRACKERINDEX, 0), "DenseBlock", precursors=[(TRACKERINDEX-1, 0)], ignore_activation=True)
        self.add_module("marker", marker)
        precursors = []

        for i in range(num_layers):
            layer = _DenseLayer(
                num_input_features + i * growth_rate,
                growth_rate=growth_rate,
                bn_size=bn_size,
                drop_rate=drop_rate,
                memory_efficient=memory_efficient,
            )
            self.add_module('denselayer%d' % (i + 1), layer)
            TRACKERINDEX += 1
            tracker_second_index += 1
            pos = (TRACKERINDEX, tracker_second_index)
            precursors.append(pos)
            tracker = TrackerModule(pos, "DenseLayer", tracked_module=layer, precursors=[(tracker_base_index, 0)])
            self.add_module('tracker%d' % (i + 1), tracker)

        TRACKERINDEX += 1
        tracker = TrackerModule((TRACKERINDEX, 0), "DenseLayer", precursors=precursors)
        self.add_module('tracker_final', tracker)
        

    def forward(self, init_features):
        features = [init_features]
        new_features = features
        for i, layer in enumerate(self.values()):
            if isinstance(layer, _DenseLayer):
                new_features = layer(features)
                features.append(new_features)
            elif isinstance(layer, TrackerModule):
                if i<len(self)-1:
                    new_features = layer(new_features)
                else:
                    #this is the last module
                    features = torch.cat(features, 1)
                    features = layer(features)
            else:
                raise ValueError
        return features


class _Transition(nn.Module):
    def __init__(self, num_input_features, num_output_features):
        super().__init__()
        self.norm = nn.BatchNorm2d(num_input_features)
        self.relu = nn.ReLU(inplace=True)
        self.conv = nn.Conv2d(num_input_features, num_output_features,
                                          kernel_size=1, stride=1, bias=False)
        self.pool = nn.AvgPool2d(kernel_size=2, stride=2)
        
        global TRACKERINDEX
        TRACKERINDEX += 1
        self.marker = TrackerModule((TRACKERINDEX, 0), "Transition", precursors=[(TRACKERINDEX-1, 0)], ignore_activation=True)
        TRACKERINDEX += 1
        self.tracker1 = TrackerModule((TRACKERINDEX, 0), "Transition Output", tracked_module=self.conv, precursors=[(TRACKERINDEX-1, 0)])

    
    def forward(self, x):
        x = self.marker(x)
        x = self.norm(x)
        x = self.relu(x)
        x = self.conv(x)
        x = self.pool(x)
        x = self.tracker1(x)
        return x


class DenseNetTorchVision(nn.Module):
    r"""Densenet-BC model class, based on
    `"Densely Connected Convolutional Networks" <https://arxiv.org/pdf/1608.06993.pdf>`_

    Args:
        growth_rate (int) - how many filters to add each layer (`k` in paper)
        block_config (list of 4 ints) - how many layers in each pooling block
        num_init_features (int) - the number of filters to learn in the first convolution layer
        bn_size (int) - multiplicative factor for number of bottle neck layers
          (i.e. bn_size * k features in the bottleneck layer)
        drop_rate (float) - dropout rate after each dense layer
        num_classes (int) - number of classification classes
        memory_efficient (bool) - If True, uses checkpointing. Much more memory efficient,
          but slower. Default: *False*. See `"paper" <https://arxiv.org/pdf/1707.06990.pdf>`_
    """

    __constants__ = ['features']

    def __init__(self, growth_rate=32, block_config=(6, 12, 24, 16),
                 num_init_features=64, num_transition_features=[], bn_size=4, drop_rate=0, num_classes=1000, memory_efficient=False):

        super().__init__()
        self.ID = 'Backbone'
        # First convolution
        self.growth_rate = growth_rate

        global TRACKERINDEX
        TRACKERINDEX = 0 # set 0 because TRACKERINDEX will be shared across all instances of this network and there will be issues if there is more than 1 instance 
        tracker1 = TrackerModule((TRACKERINDEX, 0), "input", precursors=[])

        first_layer = nn.Conv2d(3, num_init_features, kernel_size=7, stride=2, padding=3, bias=False)
        first_layer.ID = self.ID + '_first_layer'

        TRACKERINDEX += 1
        tracker2 = TrackerModule((TRACKERINDEX, 0), "conv1", tracked_module=first_layer, precursors=[(TRACKERINDEX-1, 0)])
        TRACKERINDEX += 1
        tracker3 = TrackerModule((TRACKERINDEX, 0), "maxpool", precursors=[(TRACKERINDEX-1, 0)])

        self.features = nn.Sequential(OrderedDict([
            ('tracker1', tracker1),
            ('conv0', nn.Conv2d(3, num_init_features, kernel_size=7, stride=2, padding=3, bias=False)),
            ('norm0', nn.BatchNorm2d(num_init_features)),
            ('relu0', nn.ReLU(inplace=True)),
            ('tracker2', tracker2),
            ('pool0', nn.MaxPool2d(kernel_size=3, stride=2, padding=1)),
            ('tracker3', tracker3),
        ]))

        self.relu2 = nn.ReLU()

        # Each denseblock
        num_features = num_init_features
        for i, num_layers in enumerate(block_config):
            block = _DenseBlock(
                num_layers=num_layers,
                num_input_features=num_features,
                bn_size=bn_size,
                growth_rate=growth_rate,
                drop_rate=drop_rate,
                memory_efficient=memory_efficient
            )
            self.features.add_module('denseblock%d' % (i + 1), block)
            num_features = num_features + num_layers * growth_rate
            if i != len(block_config) - 1:
                num_output_features = num_transition_features[i] if num_transition_features else num_features // 2
                trans = _Transition(num_input_features=num_features, num_output_features=num_output_features)
                self.features.add_module('transition%d' % (i + 1), trans)
                num_features = num_output_features

        # Final batch norm
        self.features.add_module('norm5', nn.BatchNorm2d(num_features))

        TRACKERINDEX += 1
        self.tracker4 = TrackerModule((TRACKERINDEX, 0), "avgpool", precursors=[(TRACKERINDEX-1, 0)])
        TRACKERINDEX += 1
        self.marker = TrackerModule((TRACKERINDEX, 0), "Flatten to 1D vector", precursors=[(TRACKERINDEX-1, 0)], ignore_activation=True)
        self.classifier = nn.Linear(num_features, num_classes)
        TRACKERINDEX += 1
        self.tracker5 = TrackerModule((TRACKERINDEX, 0), "output", precursors=[(TRACKERINDEX-1, 0)])

        # Official init from torch repo.
        for m in self.modules():
            if isinstance(m, nn.Conv2d):
                nn.init.kaiming_normal_(m.weight)
            elif isinstance(m, nn.BatchNorm2d):
                nn.init.constant_(m.weight, 1)
                nn.init.constant_(m.bias, 0)
            elif isinstance(m, nn.Linear):
                nn.init.constant_(m.bias, 0)

    def forward(self, x):
        features = self.features(x)
        
        out = self.relu2(features)
        out = F.adaptive_avg_pool2d(out, (1, 1))
        out = torch.flatten(out, 1)
        out = self.tracker4(out)
        out = self.classifier(out)
        out = self.tracker5(out)
        return out


def _load_state_dict(model, model_url, progress):
    # '.'s are no longer allowed in module names, but previous _DenseLayer
    # has keys 'norm.1', 'relu.1', 'conv.1', 'norm.2', 'relu.2', 'conv.2'.
    # They are also in the checkpoints in model_urls. This pattern is used
    # to find such keys.
    pattern = re.compile(
        r'^(.*denselayer\d+\.(?:norm|relu|conv))\.((?:[12])\.(?:weight|bias|running_mean|running_var))$')

    incompatible = []
    current_model_dict = model.state_dict()
    state_dict = load_state_dict_from_url(model_url, progress=progress)
    for key in list(state_dict.keys()):
        res = pattern.match(key)
        if res:
            new_key = res.group(1) + res.group(2)
            state_dict[new_key] = state_dict[key]
            del state_dict[key]
        if key in current_model_dict and state_dict[key].size()!=current_model_dict[key].size():
            del state_dict[key]
            incompatible.append(key)

    print('Mismatching parameters or buffers')
    print(incompatible)
    missing = model.load_state_dict(state_dict, strict=False)
    print('Loading weights from statedict. Missing and unexpected keys:')
    print(missing)
    


def _densenet(arch, growth_rate, block_config, num_init_features, pretrained, progress,
              **kwargs):
    model = DenseNetTorchVision(growth_rate, block_config, num_init_features, **kwargs)
    if pretrained:
        _load_state_dict(model, model_urls[arch], progress)
    return model


def densenet121(pretrained=False, progress=True, **kwargs):
    r"""Densenet-121 model from
    `"Densely Connected Convolutional Networks" <https://arxiv.org/pdf/1608.06993.pdf>`_

    Args:
        pretrained (bool): If True, returns a model pre-trained on ImageNet
        progress (bool): If True, displays a progress bar of the download to stderr
        memory_efficient (bool) - If True, uses checkpointing. Much more memory efficient,
          but slower. Default: *False*. See `"paper" <https://arxiv.org/pdf/1707.06990.pdf>`_
    """
    return _densenet('densenet121', 32, (6, 12, 24, 16), 64, pretrained, progress,
                     **kwargs)



def densenet161(pretrained=False, progress=True, **kwargs):
    r"""Densenet-161 model from
    `"Densely Connected Convolutional Networks" <https://arxiv.org/pdf/1608.06993.pdf>`_

    Args:
        pretrained (bool): If True, returns a model pre-trained on ImageNet
        progress (bool): If True, displays a progress bar of the download to stderr
        memory_efficient (bool) - If True, uses checkpointing. Much more memory efficient,
          but slower. Default: *False*. See `"paper" <https://arxiv.org/pdf/1707.06990.pdf>`_
    """
    return _densenet('densenet161', 48, (6, 12, 36, 24), 96, pretrained, progress,
                     **kwargs)



def densenet169(pretrained=False, progress=True, **kwargs):
    r"""Densenet-169 model from
    `"Densely Connected Convolutional Networks" <https://arxiv.org/pdf/1608.06993.pdf>`_

    Args:
        pretrained (bool): If True, returns a model pre-trained on ImageNet
        progress (bool): If True, displays a progress bar of the download to stderr
        memory_efficient (bool) - If True, uses checkpointing. Much more memory efficient,
          but slower. Default: *False*. See `"paper" <https://arxiv.org/pdf/1707.06990.pdf>`_
    """
    return _densenet('densenet169', 32, (6, 12, 32, 32), 64, pretrained, progress,
                     **kwargs)



def densenet201(pretrained=False, progress=True, **kwargs):
    r"""Densenet-201 model from
    `"Densely Connected Convolutional Networks" <https://arxiv.org/pdf/1608.06993.pdf>`_

    Args:
        pretrained (bool): If True, returns a model pre-trained on ImageNet
        progress (bool): If True, displays a progress bar of the download to stderr
        memory_efficient (bool) - If True, uses checkpointing. Much more memory efficient,
          but slower. Default: *False*. See `"paper" <https://arxiv.org/pdf/1707.06990.pdf>`_
    """
    return _densenet('densenet201', 32, (6, 12, 48, 32), 64, pretrained, progress,
                     **kwargs)