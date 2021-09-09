import falcon
import DLWebServer

app = application = falcon.App()

app.add_route('/testshortresource', DLWebServer.TestShortResource())
app.add_route('/testlongresource', DLWebServer.TestLongResource())

app.add_route('/network', DLWebServer.NetworkResource())
app.add_route('/network/activation/layerid/{layerid:int}', DLWebServer.NetworkActivationImageResource())
app.add_route('/network/prepareforinput', DLWebServer.NetworkPrepareForInputResource())

app.add_route('/data/images', DLWebServer.DataImagesResource())