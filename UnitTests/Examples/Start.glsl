// 'Title' dean_the_coder (Twitter: @deanthecoder)
// URL (YouTube: URL)

// License: Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License

#define MIN_DIST		 .0002
#define START_DIST       1.0
#define MAX_DIST		 64.0
#define MAX_STEPS		 120.0
#define MAX_RDIST		 20.0
#define MAX_RSTEPS		 64.0
#define SHADOW_STEPS	 30.0
#define MAX_SHADOW_DIST  20.0

#define SHADOW_BLUR      0.8
#define AO_STRENGTH      0.5
#define FOG_STRENGTH     0.1
#define FOG_ADJUST_FOR_REFLECTIONS 0.2
#define GLASS_SSS        0.1
#define GLASS_SSS_DEPTH  0.2
#define REFLECT_BOUNCES  2.0

// out_rgb^2.2 / light_rgb = in_rgb
// temperature to light - http://planetpixelemporium.com/tutorialpages/light.html
#define LIGHT_RGB vec3(2, 1.6, 1.4) // 2, 1.8, 1.7 - White light

#define R	iResolution
#define Z0	min(iTime, 0.)
#define I0	min(iFrame, 0)
#define sat(x)	clamp(x, 0., 1.)
#define S  smoothstep
#define S01(a) S(0.0, 1.0, a)

float t;
vec2 g; // todo - Remove? make float?
vec4 m; // todo - Remove
bool hitGlass; // todo - remove if there's no glass.
vec3 lp;

struct Hit {
	float d;
	float id;  // todo - Needed?
	vec3 p; // todo - Needed?
};

void U(inout Hit h, float d, float id, vec3 p) {
    if (d < h.d) h = Hit(d, id, p);
}

float min2(vec2 v) { return min(v.x, v.y); }
float min3(vec3 v) { return min(v.x, min(v.y, v.z)); }
float min4(vec4 v) { return min2(min(v.xy, v.zw)); }
float max2(vec2 v) { return max(v.x, v.y); }
float max3(vec3 v) { return max(v.x, max(v.y, v.z)); }
float max4(vec4 v) { return max2(max(v.xy, v.zw)); }
float dot2(vec2 v) { return dot(v, v); }
float dot3(vec3 v) { return dot(v, v); }
float dot4(vec3 v) { return dot(v, v); }
float sum2(vec2 v) { return dot(v, vec2(1)); }
float sum3(vec3 v) { return dot(v, vec3(1)); }
float sum4(vec4 v) { return dot(v, vec4(1)); }
float mul2(vec2 v) { return v.x * v.y; }
float mul3(vec3 v) { return v.x * v.y * v.z; }
float mul4(vec4 v) { return mul2(v.xy * v.zw); }
float spow(float n, float p) { return sign(n) * pow(abs(n), p); }
vec2 spow2(vec2 n, vec2 p) { return sign(n) * pow(abs(n), p); }
vec3 spow3(vec3 n, vec3 p) { return sign(n) * pow(abs(n), p); }
vec4 spow4(vec4 n, vec4 p) { return sign(n) * pow(abs(n), p); }

///////////////////////////////////////////////////////////////////////////////
// Hash/noise functions (Thnx Dave_Hoskins, Shane, iq)

float h11(float p) { p = fract(p * .1031); p *= p + 3.3456; return fract(p * (p + p)); }
vec2 h22(vec2 p)
{
	vec3 v = fract(p.xyx * vec3(.1031, .1030, .0973));
    v += dot(v, v.yzx + 333.33);
    return fract((v.xx + v.yz) * v.zy);
}

vec3 h33(vec3 p)
{
	p = fract(p * vec3(.1031, .1030, .0973));
    p += dot(p, p.zxy + 333.33);
    return fract((p.xxy + p.yzz) * p.zyx);
}

vec4 h44(vec4 p)
{
	p = fract(p * vec4(.1031, .1030, .0973, .1099));
    p += dot(p, p.wzxy + 333.33);
    return fract((p.xxyz + p.yzzw) * p.zywx);
}

float h31(vec3 p)
{
    p  = fract( p*0.3183099+.1 );
	p *= 17.0;
    return fract( p.x*p.y*p.z*(p.x+p.y+p.z) );
}

float h21(vec2 p) {
    vec3 p3 = fract(vec3(p.xyx) * vec3(.1031,.11369,.13787));
    p3 += dot(p3, p3.yzx + 19.19);
    return fract((p3.x + p3.y) * p3.z);
}

float n21(vec2 p)
{
    vec2 i = floor(p);
    vec2 f = fract(p);
    f = f*f*(3.0-2.0*f);
	
    return mix(mix( h21(i), 
                        h21(i + vec2(1,0)),f.x),
                   mix( h21(i + vec2(0,1)), 
                        h21(i + 1.0),f.x),f.y);
}

float n31(vec3 p) {
    vec3 i = floor(p);
    vec3 f = fract(p);
    f = f*f*(3.0-2.0*f);
	
    return mix(mix(mix( h31(i), 
                        h31(i+vec3(1,0,0)),f.x),
                   mix( h31(i+vec3(0,1,0)), 
                        h31(i+vec3(1,1,0)),f.x),f.y),
               mix(mix( h31(i+vec3(0,0,1)), 
                        h31(i+vec3(1,0,1)),f.x),
                   mix( h31(i+vec3(0,1,1)), 
                        h31(i+1.0),f.x),f.y),f.z);
}

// Two n31 results from two scales.
vec2 n331(vec3 p, vec2 s) {
    vec2 ns;
    for (int i = I0; i < 2; i++)
        ns[i] = n31(p * s[i]);
    return ns;
}

// Three n31 results from three scales.
vec3 n3331(vec3 p, vec3 s) {
    vec3 ns;
    for (int i = I0; i < 3; i++)
        ns[i] = n31(p * s[i]);
    return ns;
}

// roughness: (0.0, 1.0], default: 0.5
// Returns unsigned noise [0.0, 1.0]
float fbm2(vec2 p, int octaves, float roughness)
{
    float sum = 0.0, amp = 1., tot = 0.0;
    roughness = sat(roughness);
    for (int i = I0; i < octaves; i++)
    {
        sum += amp * n21(p);
        tot += amp;
        amp *= roughness;
        p *= 2.0;
    }
    
	return sum / tot;
}

// roughness: (0.0, 1.0], default: 0.5
// Returns unsigned noise [0.0, 1.0]
float fbm(vec3 p, int octaves, float roughness)
{
    float sum = 0.0, amp = 1., tot = 0.0;
    roughness = sat(roughness);
    while (octaves-- > 0)
    {
        sum += amp * n31(p);
        tot += amp;
        amp *= roughness;
        p *= 2.0;
    }
    
	return sum / tot;
}

vec3 randomPos(float seed)
{
  vec4 s = vec4(seed, 0, 1, 2);
  return vec3(h21(s.xy), h21(s.xz), h21(s.xw)) * 100.0 + 100.;
}

// Returns unsigned noise [0.0, 1.0]
float fbmDistorted(vec3 p, int octaves, float roughness, float distortion)
{
  p += (vec3(n31(p + randomPos(0.0)),
              n31(p + randomPos(1.0)),
              n31(p + randomPos(2.0))) * 2. - 1.) * distortion;
  return fbm(p, octaves, roughness);
}

// vec3: detail(/octaves), dimension(/inverse contrast), lacunarity
// Returns signed noise.
float musgraveFbm(vec3 p, float octaves, float dimension, float lacunarity) {
    float sum = 0.0, amp = 1.0;
    float pwMul = pow(lacunarity, -dimension);
    while (octaves-- > 0.0) {
        float n = n31(p) * 2. - 1.;
        sum += n * amp;
        amp *= pwMul;
        p *= lacunarity;
    }

    return sum;
}

// Wave noise along X axis.
vec3 waveFbmX(vec3 p, float distort, int detail, float detailScale, float roughness) {
    float n = p.x * 20.0;
    n += distort != 0.0 ? distort * fbm(p * detailScale, detail, roughness) : 0.0;
    return vec3(sin(n) * 0.5 + 0.5, p.yz);
}

// 2D voronoi noise.
float voronoi(vec2 p) {
	vec2 o,
	     f = floor(p);
	p -= f;
	vec3 d = vec3(2);
	for (int y = -1; y <= 1; y++) {
		for (int x = -1; x <= 1; x++) {
			o = vec2(x, y);
			o += h22(f + o) - p;
			d.z = dot(o, o);
			d.y = max(d.x, min(d.y, d.z));
			d.x = min(d.x, d.z);
		}
	}

	return d.y - d.x;
}

///////////////////////////////////////////////////////////////////////////////
// Math

// Smooth abs()
float sabs(float f, float s) { return sqrt(f * f + s); }

// Smooth min()
float smin(float a, float b, float k) {
	float h = sat(.5 + .5 * (b - a) / k);
	return mix(b, a, h) - k * h * (1. - h);
}

float remap01(float f, float in1, float in2) {
	return sat((f - in1) / (in2 - in1));
}

float remap(float f, float in1, float in2, float out1, float out2) {
	return mix(out1, out2, remap01(f, in1, in2));
}

// Dip below 0, then shoot up to 1.
float easeInBack(float x) {
    const float c1 = 1.70158, c3 = c1 + 1.0;
    return c3 * x * x * x - c1 * x * x;
}

// Dip below 0, overshoot 1, back down to 1.
float backInOut(float x) {
    float f = x < 0.5 ? 2.0 * x : 1.0 - (2.0 * x - 1.0),
        g = pow(f, 3.0) - f * sin(f * 3.141);
    return x < 0.5 ? 0.5 * g : 0.5 * (1.0 - g) + 0.5;
}

// Ray/plane intersection.
bool intPlane(vec3 p0, vec3 n, vec3 ro, vec3 rd, out float t)
{ 
    float denom = dot(n, rd);
    t = dot(p0 - ro, n) / denom;
    return t >= 0.0 && abs(denom) > 0.0001;
}

bool intSphere(vec3 s0, float r, vec3 ro, vec3 rd, out float t) {
    vec3 sr = ro - s0;
    float b = 2.0 * dot(rd, sr),
          c = dot(sr, sr) - r * r,
          dis = b * b - 4.0 * c;
    if (dis < 0.0)
        return false;
        
    t = min2(-b + sqrt(dis) * vec2(-1, 1)) * 0.5;
    return true;
}

///////////////////////////////////////////////////////////////////////////////
// Space manipulation.

mat2 rot(float a) {
    float c = cos(a), s = sin(a);
	return mat2(c, s, -s, c);
}

// todo - Calling prevents precalc of rot(a) - favor use when passing into a function.
// Rotate around z axis.
vec3 rxy(vec3 p, float a) { p.xy *= rot(a); return p; }

// todo - Calling prevents precalc of rot(a) - favor use when passing into a function.
// Rotate around y axis.
vec3 rxz(vec3 p, float a) { p.xz *= rot(a); return p; }

// todo - Calling prevents precalc of rot(a) - favor use when passing into a function.
// Rotate around x axis.
vec3 ryz(vec3 p, float a) { p.yz *= rot(a); return p; }

vec3 rotAx(vec3 p, vec3 ax, float a) {
	// Thanks Blackle.
	return mix(dot(ax, p) * ax, p, cos(a)) + cross(ax, p) * sin(a);
}

vec3 dx(vec3 p, float e) { p.x += e; return p; }
vec3 dy(vec3 p, float e) { p.y += e; return p; }
vec3 dz(vec3 p, float e) { p.z += e; return p; }

// Return abs(p.x) with offset.
vec3 ax(vec3 p, float d) {
    return vec3(abs(p.x) - d, p.yz);
}

// Return abs(p.y) with offset.
vec3 ay(vec3 p, float d) {
    return vec3(p.x, abs(p.y) - d, p.z);
}

// Return abs(p.z) with offset.
vec3 az(vec3 p, float d) {
    return vec3(p.xy, abs(p.z) - d);
}

// Return smooth abs(p.x) with offset.
vec3 sax(vec3 p, float d, float s) {
    return vec3(sabs(p.x, s) - d, p.yz);
}

// Return smooth abs(p.y) with offset.
vec3 say(vec3 p, float d, float s) {
    return vec3(p.x, sabs(p.y, s) - d, p.z);
}

// Return smooth abs(p.z) with offset.
vec3 saz(vec3 p, float d, float s) {
    return vec3(p.xy, sabs(p.z, s) - d);
}

// 1D infinite repeat.
float rep(float p, float c) {
    return p - c * floor(p / c + 0.5);
}

// 2D infinite repeat.
vec2 rep2(vec2 p, vec2 c) {
    return p - c * floor(p / c + 0.5);
}

// 3D infinite repeat.
vec3 rep3(vec3 p, vec3 c) {
    return p - c * floor(p / c + 0.5);
}

// 1D limited repeat.
float repLim(float p, float c, float l) {
    return p - c * clamp(floor(p / c + 0.5), -l, l);
}

// 2D limited repeat.
vec2 repLim2(vec2 p, vec2 c, vec2 l) {
    return p - c * clamp(floor(p / c + 0.5), -l, l);
}

// 3D limited repeat.
vec3 repLim3(vec3 p, vec3 c, vec3 l) {
    return p - c * clamp(floor(p / c + 0.5), -l, l);
}

// Polar/circular repeat.
vec2 opModPolar(vec2 p, float n, float o)
{
	float angle = 3.141 / n,
		  a = mod(atan(p.x, p.y) + angle + o, 2. * angle) - angle;
	return length(p) * vec2(cos(a), sin(a));
}

float opUnionStairs(float a, float b, float r, float n) {
    // Thanks Mercury - http://mercury.sexy/hg_sdf/
	float s = r/n;
	float u = b-r;
	return min(min(a,b), 0.5 * (u + a + abs ((mod (u - a + s, 2. * s)) - s)));
}

// Convert 2D sdf into 3D sdf.
float insulate(vec3 p, float sdf2d)
{
    float dp = p.z; // distance to plane
    return sqrt(dp*dp+sdf2d*sdf2d);
}

// Bend space along the x axis.
vec3 bend(vec3 p, float k)
{
    float c = cos(k*p.x);
    float s = sin(k*p.x);
    p.xy *= mat2(c,s,-s,c);
    return p;
}

///////////////////////////////////////////////////////////////////////////////
// SDFs

float box(vec3 p, vec3 b) {
	vec3 q = abs(p) - b;
	return length(max(q, 0.)) + min(max3(q), 0.); // todo - RHS needed?
}

float box2d(vec2 p, vec2 b) {
	vec2 q = abs(p) - b;
	return length(max(q, 0.)) + min(max2(q), 0.); // todo - RHS needed?
}

float boxFrame(vec3 p, vec3 b, float e) {
	p = abs(p) - b;
	vec3 q = abs(p + e) - e;
    vec3 v1 = vec3(q.xz, p.y), v2 = vec3(q.xy, p.z), v3 = vec3(q.yz, p.x);
	return min(min(
               length(max(v3, 0.0)) + min(max3(v3), 0.0),
               length(max(v1, 0.0)) + min(max3(v1), 0.0)),
               length(max(v2, 0.0)) + min(max3(v2), 0.0)
           );
}

float cyl(vec3 p, vec2 hr) {
	vec2 d = abs(vec2(length(p.xy), p.z)) - hr;
	return min(max(d.x, d.y), 0.) + length(max(d, 0.));
}

float ellipse(vec3 p, vec3 r)
{
    float k1 = length(p / r), k2 = length(p / (r * r));
    return k1 * (k1 - 1.0) / k2;
}

float cap(vec3 p, float h, float r) {
	p.x -= clamp(p.x, 0., h);
	return length(p) - r;
}

float capTor(vec3 p, vec2 sc, float r)
{
    p.x = abs(p.x);
    float k = sc.y*p.x>sc.x*p.y ? dot(p.xy,sc) : length(p.xy);
    return sqrt( dot(p,p) + r*r - 2.0*r*k );
}

float capTorA(vec3 p, float a, float r)
{
    return capTor(p, vec2(sin(a),cos(a)), r);
}

float tor(vec3 p, vec2 r) {
	vec2 q = vec2(length(p.xz) - r.x, p.y);
	return length(q) - r.y;
}

float arc(vec3 p, float l, float a, float w, float tap)
{
	vec2 sc = vec2(sin(a), cos(a));
	float ra = 0.5 * l / a;
	p.x -= ra;

	vec2 q = p.xy - 2.0 * sc * max(0.0, dot(sc, p.xy));

	float u = abs(ra) - length(q);
	float d2 = (q.y < 0.0) ? dot(q + vec2(ra, 0.0), q + vec2(ra, 0.0)) : u * u;
	float s = sign(a);
	float t = (p.y > 0.0) ? atan(s * p.y, -s * p.x) * ra : (s * p.x < 0.0) ? p.y : l - p.y;
	return sqrt(d2 + p.z * p.z) - max(0.001, w - t * tap);
}

float cone(vec3 p, float h, float r1, float r2) {
	vec2 q = vec2(length(p.xz), p.y),
	     k1 = vec2(r2, h),
	     k2 = vec2(r2 - r1, 2. * h),
	     ca = vec2(q.x - min(q.x, (q.y < 0.) ? r1 : r2), abs(q.y) - h),
	     cb = q - k1 + k2 * clamp(dot(k1 - q, k2) / dot(k2, k2), 0., 1.);
	return ((cb.x < 0. && ca.y < 0.) ? -1. : 1.) * sqrt(min(dot(ca, ca), dot(cb, cb)));
}

// Two combined twisting cubes with infinite length.
float twisty(vec2 p, float l, float s) {
    return min(box2d(p.xy * rot(l), vec2(s)), box2d(p.xy * rot(-l), vec2(s)));
}

float honk(inout vec3 p, mat2 rot, vec2 r) {
	p.xy *= rot;
	float d = cap(p, r.x, r.y);
	p.x -= r.x;
	return d;
}

// iq's 2D N-gon SDF function.
float ngon(vec2 p, float r, int n) {
	float an = 6.2831853 / float(n);
	float he = r * tan(0.5 * an);
	p = -p.yx;
	float bn = an * floor((atan(p.y, p.x) + 0.5 * an) / an);
	vec2 cs = vec2(cos(bn), sin(bn));
	p = mat2(cs.x, -cs.y, cs.y, cs.x) * p;
	return length(p - vec2(r, clamp(p.y, -he, he))) * sign(p.x - r);
}

float hex(vec2 p, float r) {
	p = abs(p);
	return -step(max(dot(p, normalize(vec2(1, 1.73))), p.x), r);
}

float hex3D(vec3 p, vec2 h) {
	const vec3 k = vec3(-.8660254, .5, .57735);
	p = abs(p);
	p.xy -= 2. * min(dot(k.xy, p.xy), 0.) * k.xy;
	vec2 d = vec2(length(p.xy - vec2(clamp(p.x, -k.z * h.x, k.z * h.x), h.x)) * sign(p.y - h.x), p.z - h.y);
	return min(max2(d), 0.) + length(max(d, 0.));
}

// Flat 3D octagon.
float oct3D(vec3 p, float r, float h) {
	const vec3 k = vec3(-.92387953, .38268343, .41421356);
	p = abs(p);
	p.xy -= 2. * min(dot(k.xy, p.xy), 0.) * k.xy;
	p.xy -= 2. * min(dot(vec2(-k.x, k.y), p.xy), 0.) * vec2(-k.x, k.y);
	p.xy -= vec2(clamp(p.x, -k.z * r, k.z * r), r);
	vec2 d = vec2(length(p.xy) * sign(p.y), p.z - h);
	return min(max2(d), 0.) + length(max(d, 0.));
}

vec3 rayDir(vec3 ro, vec3 lookAt, vec2 uv) {
	vec3 f = normalize(lookAt - ro),
		 r = normalize(cross(vec3(0, 1, 0), f));
	return normalize(f + r * uv.x + cross(f, r) * uv.y);
}

// Find most dominant uv coords.
vec2 proj(vec3 p, vec3 n) {
    n = abs(n);
    float m = max3(n);
    return n.x == m ? p.yz : n.y == m ? p.xz : p.xy;
}

///////////////////////////////////////////////////////////////////////////////
// Environment/textures/materials.

// Plain sky color.
vec3 skyCol(float y) {
    // return vec3(0.4, 0.5, 0.7); // todo - remove if not needed.
    float alt = 0.5, tm = 1.;
    vec3 h = 1.0 - pow(vec3(2.0), -tm * vec3(35, 14, 7));
    vec3 c = pow(vec3(max(1.0 - y * alt,0.0)),vec3(6, 3, 1.5)) * vec3(0.95, 1, 1) * h;
    c *= mix(vec3(1), vec3(1, .5, .4), sin(t * 0.2) * 0.5 + 0.5);
    
    return c;
}

// Sky with clouds.
vec3 sky(vec3 rd) {
    vec3 col = skyCol(rd.y);
    
    float SKY_ALT = 10.0, SKY_SPEED = 0.2;
    float d = SKY_ALT / rd.y;
    
    if (d < 0.0) return col;
    
    vec3 p = rd * d + vec3(1, 0.2, 1) * t * SKY_SPEED;
    p.xz *= 0.2;

    float den = 1.0;
    for (int i = 0; i < 3; i++)
        den *= exp(-0.06 * fbm(p, 4, .5));
    
    return mix(col, LIGHT_RGB, S(0.9, 1., den) * (1.0 - sat(d / MAX_DIST)));
}

vec3 starz(vec3 rd) {
    float st = t - 36.0;
    rd.xy *= rot(st * 0.08);
    
    rd *= 25.;
    vec2 s = h22(floor(rd.xy));
    return vec3(s.y * S(0.05 * s.x * s.y, 0., length(fract(rd.xy) - 0.1 - s * .8)));
}

float fakeEnv(vec3 n) {
    // Thanks Blackle.
    return length(sin(n * 2.5) * 0.5 + 0.5) / sqrt(3.);
}

// Shameless self-promotion. :)
void dtc(vec2 p, inout vec3 c) {
	if (abs(p.x) > .6 || abs(p.y) > .5) return;
	if (step(min2(abs(p - vec2(0, .2))), .08) * step(p.y, .3) * step(abs(p.x), .4) > 0.) {
		c = vec3(8);
		return;
	}

	float dc = step(.5, p.x), f;
	p.x = abs(p.x) - .46;
	f = dot(p, p);
	dc += step(f, .25) * step(.16, f);
	if (dc > 0.) c = vec3(3);
}

// Wood material. Returns rgb, depth
vec4 matWood(vec3 p) {
    float n1 = fbmDistorted(p * vec3(1, .15, .15) * 7.8, 8, .5, 1.12);
    n1 = mix(n1, 1., 0.2);
    float n2 = musgraveFbm(vec3(n1 * 4.6), 8., 0., 2.5);
    float n3 = mix(n2, n1, 0.85);

    vec3 q = waveFbmX(p * vec3(0.01, .15, .15), .4, 3, 3., 3.);
    float dirt = 1. - musgraveFbm(q, 15., 0.26, 2.4) * .4;
    float grain = 1. - S(0.2, 1.0, musgraveFbm(p * vec3(500, 6, 1), 2., 2., 2.5)) * 0.2;
    n3 *= dirt * grain;

    vec3 c1 = vec3(.032, .012, .004);
    vec3 c2 = vec3(.25, .11, .037);
    vec3 c3 = vec3(.52, .32, .19);

    float depth = grain * n3; // todo - adjust this.
    vec3 col = mix(c1, c2, remap01(n3, 0.185, 0.565));
    col = mix(col, c3, remap01(n3, 0.565, 1.));
    return vec4(col, depth);
}

// Returns sand color, affects normal and shine.
vec3 matSand(vec3 p, inout vec3 n, inout float shine) {
    // Bump.
    float s = fbmDistorted(p * 2.3, 2, 0.5, 1.);
    n.xz += s * 0.3 - .15;
    n = normalize(n);
    
    vec3 c = vec3(0.75, 0.5, 0.25);

    // Sand color (light/stones.)
    shine = fbmDistorted(p * 30.3, 2, 0.5, 0.5) * 0.3;
    c = mix(vec3(0.11,0.03,0.002), c, S(0., 0.08, shine));
    
    // Add grains.
    c = mix(c, vec3(0.5, 0.3, 0.2), S(0.5, 1.5, voronoi(p.xz * 40.)) * (1.0 - sat(p.z / 2.0)));
    
    return c;
}

// col *= texAluminium(p)
float texAluminium(vec3 p) {
    float n1 = musgraveFbm(p * 4.6, 5., 0., 3.);
    return S(-1., 1., fbm(p * 7. * n1, 4, .5));
}

// Returns scratchiness. See scratches().
// col = mix(inCol, scatchCol, textScratches(p))
float texScratches(vec3 p) {
        float sh = S(0.85, 1.0, n31(p * vec3(80, 12, 1)));
        p.xy = p.yx * rot(0.707);
        float sv = S(0.85, 1.0, n31(p * vec3(80, 10, 1)));
        return max(sh, sv);
}

// Return scratchiness, and apply bump to normal.
// Suggest also increasing shininess and tint color:
//   shine += s * 4.0;
//   c = mix(c, vec3(0.1), s);
float scratches(vec3 p, inout vec3 n) {
    float s = texScratches(p);
    vec3 dt = vec3(0.001, 0, 0),
         dn = vec3(texScratches(p + dt.xyy),
                   texScratches(p + dt.yxy),
                   texScratches(p + dt.yyx));
    n = normalize(n + dn - s);
    return s;
}

// ID.Reflectivity
#define SKY_ID     0.0
#define BOX_ID     2.3
#define GROUND_ID  3.3
#define SPHERE_ID  4.3
#define GLASS_ID   5.0

float glassSdf(vec3 p) {
    p.y += 2.;
    return tor(dy(p, -1.6), vec2(2, 0.2));
}

// todo - bail early on sdf functions.
Hit sdf(vec3 p) {
	Hit h = Hit(length(p - vec3(0, 0.5, 0)) - 1., SPHERE_ID, p);
    
    U(h, boxFrame(p + vec3(0, 1, 0), vec3(1), 0.1) - 0.02, BOX_ID, p);
    
    float d = p.y + 2.;
    d += d < 0.1 ? S(.1, .0, voronoi(p.xz)) * 0.01 : 0.0;
    U(h, d, GROUND_ID, p);
    
    if (!hitGlass)
        U(h, glassSdf(p), GLASS_ID, p);

    // Nothing is perfectly sharp.
    h.d -= 0.01;

	return h;
}

vec3 N(vec3 p, float d) {
	const float sceneAdjust = 0.05; // todo - inline
	float h = d * sceneAdjust;
	vec3 n = vec3(0);
	for (int i = I0; i < 4; i++) {
		vec3 e = .005773 * (2. * vec3(((i + 3) >> 1) & 1, (i >> 1) & 1, i & 1) - 1.);
		n += e * sdf(p + e * h).d;
	}

	return normalize(n);
}

vec3 NGlass(vec3 p) {
	vec3 n = vec3(0);
	for (int i = I0; i < 4; i++) {
		vec3 e = .005773 * (2. * vec3(((i + 3) >> 1) & 1, (i >> 1) & 1, i & 1) - 1.);
		n += e * glassSdf(p + e * .01);
	}

	return normalize(n);
}

float shadow(vec3 p, vec3 ld, vec3 n) {
    // Quick abort if light is behind the normal.
    if (dot(ld, n) < -.1) return 0.0;
    
	float s = 1., l = .01;
    float mxL = length(p - lp);

    for (float i = Z0; i < SHADOW_STEPS; i++) {
        float d = sdf(l * ld + p).d;
        s = min(s, mix(50., 7., SHADOW_BLUR) * d / l);
        l += max(0.05, d);
        if (mxL - l < 0.5 || s < 0.001) break;
    }

	return S01(s);
}

// Quick 2-level ambient occlusion.
float ao(vec3 p, vec3 n, vec2 h) {
    vec2 ao;
    for (int i = I0; i < 2; i++)
        ao[i] = sdf(h[i] * n + p).d;
    return sat(mul2((1.0 - AO_STRENGTH) + AO_STRENGTH * ao / h));
}

// Sub-surface scattering. (Thanks Evvvvil)
float sss(vec3 p, vec3 ld, float h) { return S01(sdf(h * ld + p).d / h); }

vec3 lights(vec3 p, vec3 ro, vec3 rd, vec3 n, Hit h, float fogAdjust) {
    if (h.id == SKY_ID) return sky(rd);

    float ss = 0.0, // todo - Remove if not needed.
          spe = 10.0,
          shine = 1.; // todo - Can set from texture.
	vec3 ld = normalize(lp - p), c;

    // Calculate ambient occlusion and shadows.
    float _ao = ao(p, n, vec2(.1, 1));
    float sha = shadow(p, ld, n);
    
    // todo - move into mat lookup 'ifs' below, if possible.
    vec2 ns = n331(h.p, vec2(20, 38)); // Cache noise.
         
    if (h.id == SPHERE_ID) { 
        c = vec3(0.5 - sum2(ns) * 0.05);
    } else if (h.id == BOX_ID) {
        shine = 1.0 - sum2(ns) * 0.1;
        c = vec3(0.5, 0.4, 0.3) * shine;
        ss = sss(p, -ld, 0.1) * .5;
    } else c = vec3(0.2); // todo

    vec3 l = sat(vec3(
        dot(ld, n), // Key light.
        dot(-ld.xz, n.xz), // Reverse light.
        n.y // Sky light.
        ));

    l.x *= fakeEnv(ld * 4.); // Light mask.
    l.x += ss; // Subsurface scattering.
    l.xy = 0.1 + 0.9 * l.xy; // Diffuse.
    l.yz *= _ao; // Ambient occlusion.
    l *= vec3(1, .5, .2); // Light contributions (key, reverse, sky).
    
    // Apply tinted shadows.
    l.x *= 0.1 + 0.9 * sha;
    vec3 skyTop = skyCol(1.0);
    c += skyTop * (1.0 - sha) * 0.5; // todo - needed?

    // Specular (Blinn-Phong)
    shine *= 0.5 + 0.5 * ns.x * ns.y; // todo - tweak/remove
    shine *= sha; // No specular in the shadows.
    l.x += pow(sat(dot(normalize(ld - rd), n)), spe) * shine;
    
    // Light falloff.
    // todo - tweak
    l.x *= dot(lp, lp) / (1.0 + dot(lp - p, lp - p));
    
    // Fresnel.
	  float fre = S(.6, 1., 1. + dot(rd, n)) * .25;

    // Combine the lights (key, reverse, sky).
	vec3 col = mix((sum2(l.xy) * LIGHT_RGB + l.z * skyTop) * c, skyTop, fre);
    
    // Simple fog layer.
    float fogY = -1.; // Fog surface Y.
    vec3 uv = vec3(p.xz, fogY) * 0.4 + t * vec3(.1, -.9, 0.2); // Fog uv.
    float fogTex = fbm(uv, 4, 0.5), // Fog texture.
          transitionDepth = 0.3; // Depth (distance) from fogY to max fog.
    fogY -= (1.0 - fogTex) * transitionDepth; // Surface deviation.
    float fg = S(0.0, -transitionDepth, p.y - fogY);

    // todo - Adjust weights.
    fg *= 0.1 + .2 * fogTex; // Fog texture.
    fg *= 1.0 - sat(-rd.y); // Shallow angle = More fog.
    
    // Distance Fog.
    fg += 1.0 - exp(dot3(p - ro) / -fogAdjust * mix(0.0001, 0.01, FOG_STRENGTH));

    return mix(skyCol(0.0), col, 1.0 - sat(fg));
}

float fade = 1.0;
float addFade(float a) { return min(1.0, abs(t - a)); }

vec4 march(vec3 ro, vec3 rd) {
	t = mod(iTime, 30.);
    fade = addFade(0.0); // * addfade(...);
    lp = vec3(12, 6, -20);
    
    // March the scene.
    vec3 p = ro,
         col = vec3(0),
         glassP = col, glassN;
	float d = START_DIST, i;
    hitGlass = false;
	Hit h;
	for (i = Z0; i < MAX_STEPS; i++) {
        // todo - remove if enclosed space
        if (d > MAX_DIST) { h.id = SKY_ID; break; }
        
		h = sdf(p);
		if (abs(h.d) < MIN_DIST * d) {
			if (!hitGlass && h.id == GLASS_ID) {
				hitGlass = true;
				glassP = p;
				glassN = NGlass(p);
				rd = normalize(refract(rd, glassN, .76));
				continue;
			}

			break;
		}
        
        d += h.d;
        p += h.d * rd;
	}
	
    float dof = sat(d / MAX_DIST);

	// Floaty particles.
    vec3 dv = rd;
	for (i = 1.5; i < d; i += 4.) {
		vec3 vp = ro + dv * i;
		vp.yz -= t * .05;
		g.x += .3 * (1. - S(0., mix(.05, .02, sat((i - 1.) / 19.)), length(fract(vp - ro) - .5)));
        dv.xz *= rot(0.5);
	}

    // Brighten scene when facing the sun.
    col += .2 * dof * LIGHT_RGB * S(0.5, 1.0, pow(sat(dot(rd, normalize(lp))), 3.0));

	vec3 n;
    col += g.x * LIGHT_RGB;
    col += lights(p, ro, rd, n = N(p, d), h, 1.0);

    // todo - reflections needed?
    if (fract(h.id) > 0.0 || hitGlass) {
		if (hitGlass) {
			p = glassP;
			n = glassN;
            
            // Add glass specular.
            vec3 ld = normalize(lp - p);
            col += .05 + LIGHT_RGB * pow(sat(dot(normalize(ld - rd), n)), 80.);
            
            col += GLASS_SSS * sat(glassSdf(p + normalize(ld - p) * GLASS_SSS_DEPTH) / GLASS_SSS_DEPTH);
		}

        // We hit a reflective surface, so march reflections.
        float opac = 1.0;
        for (float bounce = 0.0; bounce < REFLECT_BOUNCES && opac > 0.1; bounce++) {
            float refl = fract(h.id);

            rd = reflect(rd, n);
            p += n * 0.01;  // todo - ditch raymarch and just reflect sky?
            ro = p;
            d = 0.01;
            for (i = Z0; i < MAX_RSTEPS; i++) {
                if (d > MAX_RDIST) {
                    // todo - remove?
                    h.id = SKY_ID;
                    bounce = REFLECT_BOUNCES;
                    break;
                }
                
                h = sdf(p);
                if (abs(h.d) < MIN_DIST * d)
                    break;

                d += h.d;
                p += h.d * rd;
            }

            // Add a hint of the reflected color.
            opac *= refl;
            col += opac * (1. - col) * lights(p, ro, rd, n = N(p, d), h, FOG_ADJUST_FOR_REFLECTIONS);
        }
    }
    
    return vec4(pow(max(vec3(0), col), vec3(0.4545)), dof);
}

void mainVR(out vec4 fragColor, vec2 fc, vec3 ro, vec3 rd) {
    rd.xz *= mat2(1, 0, 0, -1);
	fragColor = march(ro, rd);
    fragColor.rgb *= fade;
}

void mainImage(out vec4 fragColor, vec2 fc)
{
    g = vec2(0);
	m = abs(iMouse / vec2(640, 360).xyxy);
    
    vec2 uv = (fc - .5 * R.xy) / R.y;
	vec3 lookAt = vec3(0, 0, 0), ro = vec3(0.00, 0.001, -6);
    
    // View bob - todo
    ro += 0.1 * sin(iTime * vec3(.9, .7, .3));

    ro.yz *= rot(m.y * 2. - 1.); ro.xz *= rot(m.x * 2. - 1.);

    vec4 rgbz = march(ro, rayDir(ro, lookAt, uv));
    vec3 col = rgbz.rgb;

    // Vignette.
    col *= 1.0 - 0.3 * dot(uv, uv);
    
	fragColor = vec4(col * fade, sat(rgbz.w));
}
