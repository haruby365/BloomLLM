from constants import repoUri, cacheDir, port

from http.server import BaseHTTPRequestHandler, HTTPServer
import json

from transformers import AutoModelForCausalLM, AutoTokenizer, set_seed
import torch

currentSeed = 2040
maxNewTokens = 20

print("Repo URI: {0}".format(repoUri))
print("Cache Dir: {0}".format(cacheDir))
print("Port: {0}".format(port))

print("Using CUDA")
torch.set_default_tensor_type(torch.cuda.FloatTensor)

print("Loading tokenizer")
tokenizer = AutoTokenizer.from_pretrained(repoUri, cache_dir=cacheDir)
print("Loading model")
model = AutoModelForCausalLM.from_pretrained(repoUri)
print("Launching server")

class RequestHandler(BaseHTTPRequestHandler):
    def sendPlainString(self, code, sendStr):
        self.send_response(code)
        self.send_header("Content-Type", "text/plain; charset=utf-8")
        self.end_headers()
        self.wfile.write(sendStr.encode(encoding="utf-8"))
        return

    def do_GET(self):
        try:
            self.sendPlainString(200, "This is a Bloom LLM text generator server")
        except Exception as e:
            self.sendPlainString(500, str(e))
        return

    def do_POST(self):
        try:
            postStr = self.rfile.read1().decode(encoding="utf-8")
            postJson = json.loads(postStr)

            if "Prompt" not in postJson:
                self.sendPlainString(400, "There is no \"Prompt\".")
                return
            prompt = str(postJson["Prompt"])
            
            global currentSeed
            if "Seed" in postJson:
                currentSeed = int(postJson["Seed"])
            set_seed(currentSeed)

            global maxNewTokens
            if "MaxNewTokens" in postJson:
                maxNewTokens = int(postJson["MaxNewTokens"])
            
            inputs = tokenizer(prompt, return_tensors="pt").to(0)
            outputs = model.generate(**inputs, use_cache=True, max_new_tokens=maxNewTokens, do_sample=True)
            message = tokenizer.batch_decode(outputs, skip_special_tokens=True)[0]
            
            outputJson = { "Prompt": prompt, "Seed": currentSeed, "MaxNewTokens": maxNewTokens, "Message": message }
            sendStr = json.dumps(outputJson)
            
            self.send_response(200)
            self.send_header("Content-Type", "application/json; charset=utf-8")
            self.end_headers()
            self.wfile.write(sendStr.encode(encoding="utf-8"))
        except Exception as e:
            self.sendPlainString(500, str(e))
        return

server = HTTPServer(("0.0.0.0", port), RequestHandler)
print("Server ready")
server.serve_forever()
