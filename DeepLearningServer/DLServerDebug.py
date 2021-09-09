import zmq
import json
import base64
import time

class DLServer(object):

    def __init__(self):
        self.context = zmq.Context()
        self.socket = self.context.socket(zmq.REP)
        self.socket.bind("tcp://*:5570")
        print("ready")

    def run(self):
        while True:
            #  Wait for next request from client
            message = self.socket.recv_multipart()
            print("Received request: %s" % message)

            if message[0]==b'TestShort':
                self.socket.send_multipart([b'TestShort', b'HulaHula'])
                print("send TestShort")

            elif message[0]==b'TestLong':
                time.sleep(50)
                self.socket.send_multipart([b'TestLong', b'HulaHulaHup'])
                print("send TestLong")
            
            else:
                raise ValueError("Could not process the message.")

dl_server = DLServer()
dl_server.run()