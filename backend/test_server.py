"""HTTP-level regression tests; uses an ephemeral port and isolated server state."""

import json
import threading
import unittest
from urllib.error import HTTPError
from urllib.request import Request, urlopen

import server


class ServerTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.http = server.ThreadingHTTPServer(("127.0.0.1", 0), server.NPCRequestHandler)
        cls.worker = threading.Thread(target=cls.http.serve_forever, daemon=True)
        cls.worker.start()
        cls.base = f"http://127.0.0.1:{cls.http.server_port}"

    @classmethod
    def tearDownClass(cls):
        cls.http.shutdown()
        cls.http.server_close()
        cls.worker.join()

    def setUp(self):
        server.command_index = 0
        server.latest_status = "idle"

    def request(self, path, data=None):
        body = None if data is None else json.dumps(data).encode()
        request = Request(self.base + path, data=body, headers={"Content-Type": "application/json"})
        try:
            response = urlopen(request, timeout=3)
        except HTTPError as error:
            response = error
        with response:
            return response.status, json.load(response)

    def test_full_sequence(self):
        for command in server.commands:
            self.assertEqual(self.request("/command"), (200, command))
            for status in ("accepted", "completed"):
                self.assertEqual(self.request("/result", {"id": command["id"], "status": status})[0], 200)
        self.assertEqual(self.request("/command"), (200, {"action": "done"}))

    def test_wrong_id_and_out_of_order_do_not_advance(self):
        for report in ({"id": 999, "status": "completed"}, {"id": 1, "status": "completed"}):
            self.assertEqual(self.request("/result", report)[0], 409)
        self.assertEqual(self.request("/status")[1]["command_index"], 0)
        self.assertEqual(self.request("/status")[1]["status"], "idle")

    def test_stale_completion_cannot_skip_next_command(self):
        for status in ("accepted", "completed"):
            self.request("/result", {"id": 1, "status": status})
        self.request("/result", {"id": 2, "status": "accepted"})
        self.assertEqual(self.request("/result", {"id": 1, "status": "completed"})[0], 409)
        self.assertEqual(self.request("/command")[1]["id"], 2)

    def test_rejection_does_not_advance(self):
        self.assertEqual(self.request("/result", {"id": 1, "status": "rejected"})[0], 200)
        self.assertEqual(self.request("/status")[1]["command_index"], 0)

    def test_invalid_payload_and_unknown_path(self):
        self.assertEqual(self.request("/result", [1, 2])[0], 400)
        self.assertEqual(self.request("/result", {"id": 1, "status": "unknown"})[0], 400)
        self.assertEqual(self.request("/missing")[0], 404)


if __name__ == "__main__":
    unittest.main()
