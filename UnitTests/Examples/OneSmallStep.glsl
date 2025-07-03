// 'One small step' dean_the_coder (Twitter: @deanthecoder)
// https://www.shadertoy.com/view/tt3yRH
//
// A recreation of the first footprint on the Moon, made by Neil Armstrong in 1969.
// Amazing to think it's still there.
//
// Code coming in at under 2.4Kb.
//
// Thanks to Evvvvil, Flopine, Nusan, BigWings, Iq, Shane
// and a bunch of others for sharing their knowledge!

float n31(vec3 p) {
	vec3 s = vec3(7, 157, 113), ip = floor(p);
	p = fract(p);
	p = p * p * (3. - 2. * p);
	vec4 h = vec4(0, s.yz, 270) + dot(ip, s);
	h = mix(fract(sin(h) * 43.5453), fract(sin(h + s.x) * 43.5453), p.x);
	h.xy = mix(h.xz, h.yw, p.y);
	return mix(h.x, h.y, p.z);
}

mat2 rot(float a) {
	float c = cos(a),
	      s = sin(a);
	return mat2(c, s, -s, c);
}

float box(vec3 p, vec3 b) {
	vec3 q = abs(p) - b;
	return length(max(q, 0.)) + min(max(q.x, max(q.y, q.z)), 0.);
}

float map(vec3 p) {
	float r, k, t, h,
	      bmp = (n31(p) + n31(p * 2.12) * .5 + n31(p * 4.42) * .25 + n31(p * 8.54) * .125 + n31(p * 16.32) * .062 + n31(p * 32.98) * .031 + n31(p * 63.52) * .0156) * .5 * (.5 + 2. * exp(-pow(length(p.xz - vec2(.5, 2.2)), 2.) * .26)),
	      a = p.y - .27 - bmp,
	      b = (bmp * bmp * .5 - .5) * .12;
	p.xy = -p.xy;
	p.x /= .95 - cos((p.z + 1.2 - sign(p.x)) * .8) * .1;
	vec3 tp = p;
	tp.z = mod(tp.z - .5, .4) - .2;
	t = max(box(tp, vec3(2, .16, .12 + tp.y * .25)), box(p - vec3(0, 0, 1.1), vec3(2, .16, 1.7)));
	tp = p;
	tp.x = abs(p.x) - 1.65;
	tp.z -= 1.1;
	t = min(t, box(tp, vec3(.53 - .12 * tp.z, .16, 1.6)));
	p.z /= cos(p.z * .1);
	vec2 q = p.xz;
	q.x = abs(q.x);
	k = q.x * .12 + q.y;
	if (k < 0.) r = length(q) - 1.2;
	else if (k > 2.48) r = length(q - vec2(0, 2.5)) - 1.5;
	else r = dot(q, vec2(.99, -.12)) - 1.2;

	b -= max(max(r, p.y), -t);
	h = clamp(.5 + .5 * (b - a) / -.8, 0., 1.);
	return mix(b, a, h) + .8 * h * (1. - h);
}

vec3 NM(vec3 p, float t) {
	vec3 n = vec3(0), e;
	for (int i = 0; i < 4; i++) {
		e = .5773 * (2. * vec3(((i + 3) >> 1) & 1, (i >> 1) & 1, i & 1) - 1.);
		n += e * map(p + .005 * t * e);
	}

	return normalize(n);
}

float ao(vec3 p, vec3 n, float h) { return map(p + h * n) / h; }

vec3 lights(vec3 p, vec3 rd, float d) {
	vec3 ld = normalize(vec3(6, 3, -10) - p),
	     n = NM(p, d) + n31(p * 79.0625) * .25 - .25;
	float ao = .1 + .9 * dot(vec3(ao(p, n, .1), ao(p, n, .4), ao(p, n, 2.)), vec3(.2, .3, .5)),
	      l1 = max(0., .1 + .9 * dot(ld, n)),
	      l2 = max(0., .1 + .9 * dot(ld * vec3(-1, 0, -1), n)) * .2,
	      spe = max(0., dot(rd, reflect(ld, n))) * .1,
	      fre = smoothstep(.7, 1., 1. + dot(rd, n)),
	      s = 1.,
	      t = .1;
	for (float i = 0.; i < 30.; i++) {
		float h = map(p + ld * t);
		s = min(s, 15. * h / t);
		t += h;
		if (s < .001) break;
	}

	l1 *= .1 + .9 * clamp(s, 0., 1.);
	return mix(.3, .4, fre) * ((l1 + l2) * ao + spe) * vec3(2, 1.8, 1.7);
}

float d = 0.;
vec3 march(vec3 ro, vec3 rd) {
	vec3 p;
	d = .01;
	for (float i = 0.; i < 96.; i++) {
		p = ro + rd * d;
		float h = map(p);
		if (abs(h) < .0015) break;
		d += h;
	}

	return lights(p, rd, d) * exp(-d * .14);
}

void mainImage(out vec4 fragColor, vec2 fc) {
	float t = mod(iTime * .2, 30.);
	vec2 q = fc / iResolution.xy,
	     uv = (fc - .5 * iResolution.xy) / iResolution.y;
	vec3 c, f, r,
	     ro = vec3(0, .2, -4);
	ro.yz *= rot(-sin(t * .3) * .1 - .6);
	ro.xz *= rot(1.1 + cos(t) * .2);
	f = normalize(vec3(0, 0, .8) - ro);
	r = normalize(cross(vec3(0, 1, 0), f));
	c = march(ro, normalize(f + r * uv.x + cross(f, r) * uv.y));
	c = clamp(c, vec3(0), vec3(1));
	c = pow(c, vec3(.45));
	c *= .5 + .5 * pow(16. * q.x * q.y * (1. - q.x) * (1. - q.y), .4);
	fragColor = vec4(c, mix(1.2, 0., (d + 1.) / 8.));
}