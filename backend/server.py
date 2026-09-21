"""Unity NPC 本地任务服务：启动 python backend/server.py，重启以重置任务。"""

import json
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

latest_status = "idle"
commands = [
    {"id": 1, "action": "move", "x": 3.0, "z": 0.8},
    {"id": 2, "action": "buy_food"},
    {"id": 3, "action": "move", "x": 0.0, "z": 0.0},
    {"id": 4, "action": "deliver_food"}
]

command_index = 0


class NPCRequestHandler(BaseHTTPRequestHandler):
    def do_GET(self):
        if self.path == "/health":
            self.send_json(200, {
                "status": "ok",
                "service": "ai-npc",
                "npc_count": 1
            })

        elif self.path == "/command":
            if command_index < len(commands):
                self.send_json(200, commands[command_index])
            else:
                self.send_json(200, {"action": "done"})

        elif self.path == "/status":
            self.send_json(200, {
                "status": latest_status,
                "command_index": command_index,
                "total_commands": len(commands)
            })

        else:
            self.send_json(404, {"error": "not_found"})

    def do_POST(self):
        global latest_status, command_index
        if self.path != "/result":
            self.send_json(404, {"error": "not_found"})
            return

        # Content-Length 告诉我们请求正文有多少字节。
        try:
            length = int(self.headers.get("Content-Length", "0"))
        except ValueError:
            self.send_json(400, {"error": "invalid_content_length"})
            return
        if not 0 < length <= 4096:
            self.send_json(400, {"error": "invalid_body_size"})
            return

        # 读取正文，把 JSON 转成 Python 字典。
        body = self.rfile.read(length)

        try:
            result = json.loads(body)
        except (ValueError, UnicodeDecodeError):
            self.send_json(400, {"error": "invalid_json"})
            return

        if not isinstance(result, dict):
            self.send_json(400, {"error": "expected_object"})
            return

        # 已经没有正在执行的指令。
        if command_index >= len(commands):
            self.send_json(409, {"error": "no_active_command"})
            return

        expected_id = commands[command_index]["id"]
        received_id = result.get("id")

        # 回报必须属于当前指令。
        if type(received_id) is not int or received_id != expected_id:
            self.send_json(409, {
                "error": "command_id_mismatch",
                "expected_id": expected_id,
                "received_id": received_id
            })
            return

        status = result.get("status")

        if status not in ("accepted", "rejected", "completed"):
            self.send_json(400, {"error": "invalid_status"})
            return

        if status == "completed" and latest_status != "accepted":
            self.send_json(409, {
                "error": "invalid_transition",
                "message": "必须先接受指令，再回报完成"
            })
            return

        # 当前动作被接受后，收到完成回报，才推进到下一条。
        if status == "completed" and latest_status == "accepted":
            if command_index < len(commands):
                command_index += 1

        latest_status = status

        print("收到执行结果：", result, flush=True)

        self.send_json(200, {"received": True})

    def send_json(self, status_code, data):
        # HTTP 传输的是字节，因此先把字典转为 JSON，再编码。
        body = json.dumps(data, ensure_ascii=False).encode("utf-8")
        self.send_response(status_code)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(body)))
        self.send_header("Cache-Control", "no-store")
        self.end_headers()
        self.wfile.write(body)


if __name__ == "__main__":
    # 只监听本机；这是学习阶段的服务骨架。
    server = ThreadingHTTPServer(("127.0.0.1", 8001), NPCRequestHandler)
    print("NPC service: http://127.0.0.1:8001/health", flush=True)
    print("Press Ctrl+C to stop.", flush=True)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        server.server_close()
