/*
 * Example code for the Tetra parser.
 */
float main() {
    return pi();
}

float pi() {
    float sum = 0.0;
    int sign = 1;
    int limit = 800; // Increase to improve accuracy.

    for (int i = 0; i < limit; ++i) {
        int denominator = 2 * i + 1;
        float term = 1.0 / denominator;
        term *= sign;
        sum += term;
        sign = -sign;
    }

    sum *= 4.0;

    return sum;
}
