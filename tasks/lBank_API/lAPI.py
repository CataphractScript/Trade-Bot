import asyncio
import websockets
import json

async def listen_to_lbank():
    uri = "wss://www.lbkex.net/ws/V2/"
    async with websockets.connect(uri) as websocket:
        request_message = {
            "action": "subscribe",
            "subscribe": "kbar",
            "kbar": "1min",
            "pair": "btc_usdt",
        }    
        await websocket.send(json.dumps(request_message))
        print("Request sent to WebSocket.")
        
        while True:
            response = await websocket.recv()
            print(f"Received message: {response}")

asyncio.get_event_loop().run_until_complete(listen_to_lbank())
