import type { CapacitorConfig } from '@capacitor/cli';

/**
 * O app Android do YBYRA (Capacitor): o mesmo site, dentro de uma casca Android.
 * APP_AMBIENTE escolhe o APK: "homol" conversa com o servidor de teste por http na rede local; "prod" só por https.
 * O site do navegador não usa este arquivo.
 */
const homologacao = (process.env['APP_AMBIENTE'] ?? 'homol') !== 'prod';

const config: CapacitorConfig = {
  appId: 'br.cptm.ybyra',
  appName: 'YBYRA',
  webDir: 'dist/cptm-front/browser',
  server: {
    // Em homologação o app abre por http://localhost, para poder chamar o servidor de teste (http://IP:8045) sem "conteúdo misto".
    androidScheme: homologacao ? 'http' : 'https',
  },
};

export default config;
