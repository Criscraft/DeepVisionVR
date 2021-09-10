import falcon
import DLWebServer

app = application = falcon.App()

app.add_route('/testshortresource', DLWebServer.TestShortResource())
app.add_route('/testlongresource', DLWebServer.TestLongResource())

app.add_route('/network', DLWebServer.NetworkResource())
app.add_route('/network/activation/layerid/{layerid:int}', DLWebServer.NetworkActivationImageResource())
app.add_route('/network/featurevisualization/layerid/{layerid:int}', DLWebServer.NetworkFeatureVisualizationResource())
app.add_route('/network/prepareforinput', DLWebServer.NetworkPrepareForInputResource())
app.add_route('/network/classificationresult', DLWebServer.NetworkClassificationResultResource())
app.add_route('/network/weighthistogram/layerid/{layerid:int}', DLWebServer.NetworkWeightHistogramResource())
app.add_route('/network/activationhistogram/layerid/{layerid:int}', DLWebServer.NetworkActivationHistogramResource())

app.add_route('/data/images', DLWebServer.DataImagesResource())
app.add_route('/data/noiseimage', DLWebServer.DataNoiseImageResource())
