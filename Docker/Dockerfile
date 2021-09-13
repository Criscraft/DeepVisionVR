FROM nvidia/cuda:11.0.3-cudnn8-runtime-ubuntu20.04

# Install some basic utilities
RUN apt-get update && apt-get install -y \
    ca-certificates \
    sudo \
    git \
    bzip2 \
    libx11-6 \
 && rm -rf /var/lib/apt/lists/*

RUN addgroup --gid 250 inb
RUN adduser --home /home/linse --shell /bin/bash --ingroup inb --uid 26268 linse

# Install Python 3.8
RUN apt-get update && apt-get install -y \
    python3.8 \
    python3-pip

RUN pip3 install torch==1.8.1+cu111 torchvision==0.9.1+cu111 -f https://download.pytorch.org/whl/torch_stable.html
RUN pip3 install jupyter
RUN pip3 install numpy
RUN pip3 install tqdm
RUN pip3 install Pillow
RUN pip3 install scikit-learn
RUN pip3 install pandas
RUN pip3 install matplotlib
RUN pip3 install torchsummaryX
RUN pip3 install torchcontrib
RUN pip3 install opencv-python
RUN pip3 install pyzmq
RUN pip3 install pygments
RUN pip3 install falcon
RUN pip3 install gunicorn
RUN pip3 install httpie
RUN pip3 install pytest


RUN apt-get update && DEBIAN_FRONTEND=noninteractive apt-get install -y \
    'ffmpeg' \
    'libsm6' \ 
    'libxext6'

WORKDIR /app

#make port 8888 available from outside, just in case the container is supposed to run jupyter notebooks
EXPOSE 8888
EXPOSE 5570

USER linse:inb 