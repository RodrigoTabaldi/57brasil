import http from 'k6/http';
import { check, sleep } from 'k6';

const mode = __ENV.MODE || 'smoke';
const baseUrl = __ENV.BASE_URL || 'http://host.docker.internal:3000';
const category = __ENV.CATEGORY || '';
const brand = __ENV.BRAND || '';
const query = __ENV.QUERY || 'filtro';

const profiles = {
  smoke: { executor: 'constant-vus', vus: 5, duration: '30s' },
  A: { executor: 'constant-vus', vus: 200, duration: '10m' },
  B: { executor: 'constant-vus', vus: 1000, duration: '10m' },
  C: { executor: 'ramping-vus', startVUs: 0, stages: [
    { duration: '5m', target: 200 }, { duration: '5m', target: 1000 },
    { duration: '5m', target: 2500 }, { duration: '5m', target: 5000 },
    { duration: '2m', target: 0 },
  ] },
};
if (!profiles[mode]) throw new Error('MODE must be smoke, A, B or C');

export const options = {
  scenarios: { catalog: profiles[mode] },
  thresholds: {
    'http_req_duration{kind:catalog}': ['p(95)<300'],
    'http_req_duration{kind:filter}': ['p(95)<500'],
    http_req_failed: ['rate<0.01'],
  },
};

export default function () {
  const choice = Math.random();
  let path = '/api/v1/products?page=1';
  let kind = 'catalog';
  if (choice < 0.3) {
    path += `&q=${encodeURIComponent(query)}`;
    kind = 'filter';
  } else if (choice < 0.6 && (category || brand)) {
    if (category) path += `&category=${encodeURIComponent(category)}`;
    if (brand) path += `&brand=${encodeURIComponent(brand)}`;
    kind = 'filter';
  }
  const response = http.get(`${baseUrl}${path}`, { tags: { kind } });
  check(response, { 'catalog responded 200': (r) => r.status === 200 });
  sleep(Math.random() * 2 + 1);
}
