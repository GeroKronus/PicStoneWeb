/**
 * EDITOR DE IMAGENS - Filtros Canvas API
 *
 * Implementação dos 10 filtros de imagem usando Canvas API nativo
 * Performance otimizada com TypedArrays e algoritmos eficientes
 *
 * Filtros implementados:
 * 1. Brilho (-100 a +100)
 * 2. Contraste (-100 a +100)
 * 3. Gama (-10 a +10)
 * 4. Saturação (-100 a +100)
 * 5. Matiz (-360 a +360)
 * 6. Vermelho (-100 a +100)
 * 7. Verde (-100 a +100)
 * 8. Azul (-100 a +100)
 * 9. Temperatura (-100 a +100)
 * 10. Sombras (-100 a +100)
 */

/**
 * Utilitário: Clamp valor entre 0-255
 */
function clamp(value) {
    return Math.max(0, Math.min(255, Math.round(value)));
}

/**
 * FILTRO 1: BRILHO
 * Adiciona/subtrai valor constante a cada canal RGB
 */
function applyBrightness(imageData, value) {
    if (value === 0) return imageData;

    const data = imageData.data;
    const len = data.length;
    const brightness = (value / 50) * 127;

    for (let i = 0; i < len; i += 4) {
        data[i]     = clamp(data[i] + brightness);
        data[i + 1] = clamp(data[i + 1] + brightness);
        data[i + 2] = clamp(data[i + 2] + brightness);
    }

    return imageData;
}

/**
 * FILTRO 2: CONTRASTE
 * Expande/comprime valores em torno do ponto médio (128)
 */
function applyContrast(imageData, value) {
    if (value === 0) return imageData;

    const data = imageData.data;
    const len = data.length;
    const contrast = (value / 50) * 128;
    const factor = (259 * (contrast + 255)) / (255 * (259 - contrast));

    for (let i = 0; i < len; i += 4) {
        data[i]     = clamp(factor * (data[i] - 128) + 128);
        data[i + 1] = clamp(factor * (data[i + 1] - 128) + 128);
        data[i + 2] = clamp(factor * (data[i + 2] - 128) + 128);
    }

    return imageData;
}

/**
 * FILTRO 3: GAMA
 * Correção gamma usando curva exponencial com lookup table
 */
function applyGamma(imageData, value) {
    if (value === 0) return imageData;

    const data = imageData.data;
    const len = data.length;

    let gamma;
    if (value > 0) {
        gamma = 1 + (value / 10);
    } else {
        gamma = 1 / (1 + Math.abs(value) / 10);
    }

    const gammaCorrection = 1 / gamma;

    // Lookup table para performance
    const lut = new Uint8ClampedArray(256);
    for (let i = 0; i < 256; i++) {
        lut[i] = Math.pow(i / 255, gammaCorrection) * 255;
    }

    for (let i = 0; i < len; i += 4) {
        data[i]     = lut[data[i]];
        data[i + 1] = lut[data[i + 1]];
        data[i + 2] = lut[data[i + 2]];
    }

    return imageData;
}

/**
 * FILTRO 4: SATURAÇÃO
 * Interpola entre grayscale e cor original
 */
function applySaturation(imageData, value) {
    if (value === 0) return imageData;

    const data = imageData.data;
    const len = data.length;
    const saturation = (value + 100) / 100;

    for (let i = 0; i < len; i += 4) {
        const r = data[i];
        const g = data[i + 1];
        const b = data[i + 2];

        const luminance = 0.299 * r + 0.587 * g + 0.114 * b;

        data[i]     = clamp(luminance + (r - luminance) * saturation);
        data[i + 1] = clamp(luminance + (g - luminance) * saturation);
        data[i + 2] = clamp(luminance + (b - luminance) * saturation);
    }

    return imageData;
}

/**
 * FILTRO 5: MATIZ (HUE SHIFT)
 * Converte RGB → HSV, ajusta H, converte de volta
 */
function applyHue(imageData, value) {
    if (value === 0) return imageData;

    const data = imageData.data;
    const len = data.length;
    const hueShift = value;

    for (let i = 0; i < len; i += 4) {
        const r = data[i] / 255;
        const g = data[i + 1] / 255;
        const b = data[i + 2] / 255;

        const max = Math.max(r, g, b);
        const min = Math.min(r, g, b);
        const delta = max - min;

        let h = 0;
        const s = max === 0 ? 0 : delta / max;
        const v = max;

        if (delta !== 0) {
            if (max === r) {
                h = ((g - b) / delta + (g < b ? 6 : 0)) / 6;
            } else if (max === g) {
                h = ((b - r) / delta + 2) / 6;
            } else {
                h = ((r - g) / delta + 4) / 6;
            }
        }

        h = (h * 360 + hueShift) % 360;
        if (h < 0) h += 360;
        h /= 360;

        const c = v * s;
        const x = c * (1 - Math.abs(((h * 6) % 2) - 1));
        const m = v - c;

        let rNew, gNew, bNew;
        const hSector = Math.floor(h * 6);

        switch (hSector) {
            case 0: [rNew, gNew, bNew] = [c, x, 0]; break;
            case 1: [rNew, gNew, bNew] = [x, c, 0]; break;
            case 2: [rNew, gNew, bNew] = [0, c, x]; break;
            case 3: [rNew, gNew, bNew] = [0, x, c]; break;
            case 4: [rNew, gNew, bNew] = [x, 0, c]; break;
            default: [rNew, gNew, bNew] = [c, 0, x]; break;
        }

        data[i]     = (rNew + m) * 255;
        data[i + 1] = (gNew + m) * 255;
        data[i + 2] = (bNew + m) * 255;
    }

    return imageData;
}

/**
 * FILTRO 6: CANAL VERMELHO
 */
function applyRed(imageData, value) {
    if (value === 0) return imageData;

    const data = imageData.data;
    const len = data.length;
    const factor = (value + 100) / 100;

    for (let i = 0; i < len; i += 4) {
        data[i] = clamp(data[i] * factor);
    }

    return imageData;
}

/**
 * FILTRO 7: CANAL VERDE
 */
function applyGreen(imageData, value) {
    if (value === 0) return imageData;

    const data = imageData.data;
    const len = data.length;
    const factor = (value + 100) / 100;

    for (let i = 0; i < len; i += 4) {
        data[i + 1] = clamp(data[i + 1] * factor);
    }

    return imageData;
}

/**
 * FILTRO 8: CANAL AZUL
 */
function applyBlue(imageData, value) {
    if (value === 0) return imageData;

    const data = imageData.data;
    const len = data.length;
    const factor = (value + 100) / 100;

    for (let i = 0; i < len; i += 4) {
        data[i + 2] = clamp(data[i + 2] * factor);
    }

    return imageData;
}

/**
 * FILTRO 9: TEMPERATURA
 * Warm (positivo): aumenta R, diminui B
 * Cool (negativo): diminui R, aumenta B
 */
function applyTemperature(imageData, value) {
    if (value === 0) return imageData;

    const data = imageData.data;
    const len = data.length;
    const temp = value;

    for (let i = 0; i < len; i += 4) {
        if (temp > 0) {
            data[i]     = clamp(data[i] + temp * 0.8);
            data[i + 2] = clamp(data[i + 2] - temp * 0.5);
        } else {
            const absTemp = Math.abs(temp);
            data[i]     = clamp(data[i] - absTemp * 0.8);
            data[i + 2] = clamp(data[i + 2] + absTemp * 0.5);
        }
    }

    return imageData;
}

/**
 * FILTRO 10: SOMBRAS
 * Detecta pixels escuros e ajusta proporcionalmente
 */
function applyShadows(imageData, value) {
    if (value === 0) return imageData;

    const data = imageData.data;
    const len = data.length;
    const shadowAdjust = value;

    for (let i = 0; i < len; i += 4) {
        const r = data[i];
        const g = data[i + 1];
        const b = data[i + 2];

        const luminance = 0.299 * r + 0.587 * g + 0.114 * b;

        if (luminance < 128) {
            const mask = (128 - luminance) / 128;
            const adjustment = shadowAdjust * mask;

            data[i]     = clamp(r + adjustment);
            data[i + 1] = clamp(g + adjustment);
            data[i + 2] = clamp(b + adjustment);
        }
    }

    return imageData;
}

/**
 * Aplica todos os filtros em sequência otimizada
 */
function applyAllFilters(imageData, filters) {
    // Ordem otimizada de aplicação
    if (filters.red !== undefined && filters.red !== 0) {
        applyRed(imageData, filters.red);
    }
    if (filters.green !== undefined && filters.green !== 0) {
        applyGreen(imageData, filters.green);
    }
    if (filters.blue !== undefined && filters.blue !== 0) {
        applyBlue(imageData, filters.blue);
    }
    if (filters.brightness !== undefined && filters.brightness !== 0) {
        applyBrightness(imageData, filters.brightness);
    }
    if (filters.contrast !== undefined && filters.contrast !== 0) {
        applyContrast(imageData, filters.contrast);
    }
    if (filters.gamma !== undefined && filters.gamma !== 0) {
        applyGamma(imageData, filters.gamma);
    }
    if (filters.saturation !== undefined && filters.saturation !== 0) {
        applySaturation(imageData, filters.saturation);
    }
    if (filters.hue !== undefined && filters.hue !== 0) {
        applyHue(imageData, filters.hue);
    }
    if (filters.temperature !== undefined && filters.temperature !== 0) {
        applyTemperature(imageData, filters.temperature);
    }
    if (filters.shadows !== undefined && filters.shadows !== 0) {
        applyShadows(imageData, filters.shadows);
    }

    return imageData;
}

// Export para uso em Worker e main thread
if (typeof module !== 'undefined' && module.exports) {
    module.exports = {
        applyBrightness,
        applyContrast,
        applyGamma,
        applySaturation,
        applyHue,
        applyRed,
        applyGreen,
        applyBlue,
        applyTemperature,
        applyShadows,
        applyAllFilters
    };
}
