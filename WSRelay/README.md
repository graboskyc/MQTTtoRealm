# WS Relay

Sometimes your stuff talks over WS rather than MQTT. This python script converts it.

To run, have python3 installed along with packages in the `requirements.txt`

Change the broker IP address and details at the top of the `__init__.py` script.

Run `python3 __init__.py` to start the converter.

I used [this test client](https://chrome.google.com/webstore/detail/websocket-test-client/fgponpodhbmadfljofbimhhlengambbn) and connected to `localhost` and any message I sent from this web app, goes to the python script and converted to MQTT to the main app.

