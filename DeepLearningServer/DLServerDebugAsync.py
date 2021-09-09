import zmq
import zmq.asyncio
import json
import base64
import time

class DLServer(object):

    def __init__(self):
        self.context = zmq.asyncio.Context()
        self.socket = self.context.socket(zmq.PULL)
        self.socket.bind("tcp://*:5570")

    async def run(self):
        msg = await self.socket.recv_multipart() # waits for msg to be ready
        reply = await async_process(msg)
        await self.socket.send_multipart(reply)
        """
        if message[0]==b'TestShort':
            self.socket.send_multipart([b'TestShort', b'HulaHula'])
            print("send TestShort")

        elif message[0]==b'TestLong':
            time.sleep(50)
            self.socket.send_multipart([b'TestLong', b'HulaHulaHup'])
            print("send TestLong")
        
        else:
            raise ValueError("Could not process the message.")
        """

    def stert(self):
        print("ready")
        asyncio.run(self.run())


dl_server = DLServer()
dl_server.start()