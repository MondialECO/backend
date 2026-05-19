// Lightweight load/smoke test. Run with k6 (https://k6.io):
//   k6 run -e BASE_URL=https://api.your-domain.com loadtest/smoke.js
//
// Verifies the app stays healthy and responsive under modest concurrency.
// Tune stages/thresholds to your expected launch traffic.
import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE = __ENV.BASE_URL || 'http://localhost:8080';

export const options = {
  stages: [
    { duration: '30s', target: 20 },  // ramp up
    { duration: '1m', target: 50 },   // sustained
    { duration: '30s', target: 0 },   // ramp down
  ],
  thresholds: {
    http_req_failed: ['rate<0.01'],          // <1% errors
    http_req_duration: ['p(95)<800'],        // 95% under 800ms
  },
};

export default function () {
  const live = http.get(`${BASE}/health/live`);
  check(live, { 'live 200': (r) => r.status === 200 });

  const ready = http.get(`${BASE}/health/ready`);
  check(ready, { 'ready 200': (r) => r.status === 200 });

  sleep(1);
}
