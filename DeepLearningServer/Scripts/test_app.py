import falcon
from falcon import testing
import json
import pytest

from Scripts.Falconapp import app


@pytest.fixture
def client():
    return testing.TestClient(app)


# pytest will inject the object returned by the "client" function
# as an additional parameter.
def test_list_images(client):
    doc = {
            'Hello': [
                {
                    'World': 'Hip Huepf'
                }
            ]
        }

    response = client.simulate_get('/testshortresource')
    result_doc = json.loads(response.content)

    assert result_doc == doc
    assert response.status == falcon.HTTP_OK