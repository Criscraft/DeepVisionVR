docker run --gpus all --name $1 --rm -v $(pwd):/workingdir -v /nfshome:/nfshome -v /data:/data --shm-size 256M -p 5570:5570 -it docker_dl_linse_image_cuda_11_0 python3 ${@:2}