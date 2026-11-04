// Minimal CORS proxy for OpenBible catalog downloads (Cloudflare Workers).
// Deploy, then set the proxy URL in OpenBible to: https://<your-worker>.workers.dev/?url={url}
// Only the official catalog hosts are allowed, so the worker cannot be used as an open proxy.

const ALLOWED_HOSTS = new Set(["alkitab-host.appspot.com", "boafiles.kejut.com"]);

export default {
  async fetch(request) {
    const cors = {
      "Access-Control-Allow-Origin": "*",
      "Access-Control-Allow-Methods": "GET, OPTIONS",
    };

    if (request.method === "OPTIONS") {
      return new Response(null, { status: 204, headers: cors });
    }

    const target = new URL(request.url).searchParams.get("url");
    let targetUrl;
    try {
      targetUrl = new URL(target);
    } catch {
      return new Response("Missing or invalid url", { status: 400, headers: cors });
    }

    if (targetUrl.protocol !== "https:" || !ALLOWED_HOSTS.has(targetUrl.hostname)) {
      return new Response("Host not allowed", { status: 403, headers: cors });
    }

    // fetch follows the appspot -> boafiles redirect; re-check the final host.
    const upstream = await fetch(targetUrl.toString(), { redirect: "follow" });
    if (!ALLOWED_HOSTS.has(new URL(upstream.url).hostname)) {
      return new Response("Redirect host not allowed", { status: 403, headers: cors });
    }

    const headers = new Headers(cors);
    headers.set("Content-Type", "application/octet-stream");
    return new Response(upstream.body, { status: upstream.status, headers });
  },
};
