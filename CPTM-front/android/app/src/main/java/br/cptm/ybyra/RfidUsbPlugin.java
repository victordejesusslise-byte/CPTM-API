package br.cptm.ybyra;

import android.app.PendingIntent;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.hardware.usb.UsbConstants;
import android.hardware.usb.UsbDevice;
import android.hardware.usb.UsbDeviceConnection;
import android.hardware.usb.UsbEndpoint;
import android.hardware.usb.UsbInterface;
import android.hardware.usb.UsbManager;
import android.os.Build;
import androidx.core.content.ContextCompat;
import com.getcapacitor.JSObject;
import com.getcapacitor.Plugin;
import com.getcapacitor.PluginCall;
import com.getcapacitor.PluginMethod;
import com.getcapacitor.annotation.CapacitorPlugin;
import java.nio.charset.StandardCharsets;

/**
 * O módulo RFID do YBYRA no USB-C (protótipo YBYRA-RFID-P0: ESP32-C3 Super Mini com USB nativo + PN532), Victor 2026-11-12.
 * Só USB: o ESP32 não usa Wi-Fi nem Bluetooth. Caminho: USB Host do Android, porta serial CDC ACM, 115200.
 * Manda linhas de texto (PING, SCAN, READ, WRITE BRxxxxxxxxxx) e devolve cada linha recebida no evento "linha"; a conexão
 * aberta ou fechada vai no evento "conexao". O ESP32-C3 aparece com o USB da Espressif (VID 0x303A), não CH340 nem CP2102.
 */
@CapacitorPlugin(name = "RfidUsb")
public class RfidUsbPlugin extends Plugin {
    private static final String PERMISSAO = "br.cptm.ybyra.PERMISSAO_USB";
    private static final int VID_ESPRESSIF = 0x303A;
    /** 115200 baud (little-endian), 1 bit de parada, sem paridade, 8 bits de dados. */
    private static final byte[] LINHA_115200 = {(byte) 0x00, (byte) 0xC2, (byte) 0x01, (byte) 0x00, (byte) 0x00, (byte) 0x00, (byte) 0x08};

    private UsbManager usb;
    private UsbDeviceConnection conexao;
    private UsbInterface comunicacao;
    private UsbInterface dados;
    private UsbEndpoint entrada;
    private UsbEndpoint saida;
    private volatile boolean lendo;
    private PluginCall esperandoPermissao;

    private final BroadcastReceiver receptor = new BroadcastReceiver() {
        @Override
        public void onReceive(Context context, Intent intent) {
            String acao = intent.getAction();
            UsbDevice aparelho = intent.getParcelableExtra(UsbManager.EXTRA_DEVICE);
            if (PERMISSAO.equals(acao)) {
                PluginCall call = esperandoPermissao;
                esperandoPermissao = null;
                if (aparelho != null && intent.getBooleanExtra(UsbManager.EXTRA_PERMISSION_GRANTED, false)) {
                    abrir(aparelho, call);
                } else if (call != null) {
                    call.reject("O acesso ao módulo não foi autorizado no Android.");
                }
            } else if (UsbManager.ACTION_USB_DEVICE_DETACHED.equals(acao)) {
                if (conexao != null) fechar();
            } else if (UsbManager.ACTION_USB_DEVICE_ATTACHED.equals(acao) && aparelho != null) {
                abrirSeAutorizado(aparelho);
            }
        }
    };

    @Override
    public void load() {
        usb = (UsbManager) getContext().getSystemService(Context.USB_SERVICE);
        IntentFilter filtro = new IntentFilter();
        filtro.addAction(PERMISSAO);
        filtro.addAction(UsbManager.ACTION_USB_DEVICE_ATTACHED);
        filtro.addAction(UsbManager.ACTION_USB_DEVICE_DETACHED);
        ContextCompat.registerReceiver(getContext(), receptor, filtro, ContextCompat.RECEIVER_EXPORTED);
    }

    /** O app foi aberto (ou trazido para a frente) porque o módulo foi ligado no USB-C. */
    @Override
    protected void handleOnNewIntent(Intent intent) {
        super.handleOnNewIntent(intent);
        if (intent != null && UsbManager.ACTION_USB_DEVICE_ATTACHED.equals(intent.getAction())) {
            UsbDevice aparelho = intent.getParcelableExtra(UsbManager.EXTRA_DEVICE);
            if (aparelho != null) abrirSeAutorizado(aparelho);
        }
    }

    @Override
    protected void handleOnDestroy() {
        fechar();
        try {
            getContext().unregisterReceiver(receptor);
        } catch (IllegalArgumentException ignorado) {
            // já tinha saído
        }
        super.handleOnDestroy();
    }

    /** Abre a porta do módulo; na primeira vez o Android pergunta se o app pode usar o aparelho USB. */
    @PluginMethod
    public void conectar(PluginCall call) {
        if (conexao != null) {
            call.resolve();
            return;
        }
        UsbDevice aparelho = acharModulo();
        if (aparelho == null) {
            call.reject("Conecte o módulo no celular.");
            return;
        }
        if (!usb.hasPermission(aparelho)) {
            esperandoPermissao = call;
            int flags = Build.VERSION.SDK_INT >= Build.VERSION_CODES.S ? PendingIntent.FLAG_MUTABLE : 0;
            Intent pedido = new Intent(PERMISSAO).setPackage(getContext().getPackageName());
            usb.requestPermission(aparelho, PendingIntent.getBroadcast(getContext(), 0, pedido, flags));
            return;
        }
        abrir(aparelho, call);
    }

    @PluginMethod
    public void desconectar(PluginCall call) {
        fechar();
        call.resolve();
    }

    /** Manda uma linha de comando (o \n vai junto). */
    @PluginMethod
    public void enviar(PluginCall call) {
        String linha = call.getString("linha", "");
        UsbDeviceConnection c = conexao;
        UsbEndpoint s = saida;
        if (c == null || s == null) {
            call.reject("Conecte o módulo no celular.");
            return;
        }
        byte[] bytes = (linha + "\n").getBytes(StandardCharsets.US_ASCII);
        if (c.bulkTransfer(s, bytes, bytes.length, 1000) < 0) call.reject("O módulo não recebeu o comando.");
        else call.resolve();
    }

    private void abrirSeAutorizado(UsbDevice aparelho) {
        if (conexao == null && ehModulo(aparelho) && usb.hasPermission(aparelho)) abrir(aparelho, null);
    }

    private boolean ehModulo(UsbDevice aparelho) {
        if (aparelho.getVendorId() == VID_ESPRESSIF) return true;
        for (int i = 0; i < aparelho.getInterfaceCount(); i++) {
            if (aparelho.getInterface(i).getInterfaceClass() == UsbConstants.USB_CLASS_CDC_DATA) return true;
        }
        return false;
    }

    /** O ESP32-C3 da Espressif; se não houver, o primeiro aparelho com porta serial CDC. */
    private UsbDevice acharModulo() {
        UsbDevice cdc = null;
        for (UsbDevice aparelho : usb.getDeviceList().values()) {
            if (aparelho.getVendorId() == VID_ESPRESSIF) return aparelho;
            if (cdc == null && ehModulo(aparelho)) cdc = aparelho;
        }
        return cdc;
    }

    private synchronized void abrir(UsbDevice aparelho, PluginCall call) {
        if (conexao != null) {
            if (call != null) call.resolve();
            return;
        }
        for (int i = 0; i < aparelho.getInterfaceCount(); i++) {
            UsbInterface itf = aparelho.getInterface(i);
            if (itf.getInterfaceClass() == UsbConstants.USB_CLASS_COMM && comunicacao == null) comunicacao = itf;
            if (itf.getInterfaceClass() == UsbConstants.USB_CLASS_CDC_DATA && dados == null) {
                UsbEndpoint in = null;
                UsbEndpoint out = null;
                for (int e = 0; e < itf.getEndpointCount(); e++) {
                    UsbEndpoint ep = itf.getEndpoint(e);
                    if (ep.getType() != UsbConstants.USB_ENDPOINT_XFER_BULK) continue;
                    if (ep.getDirection() == UsbConstants.USB_DIR_IN) in = ep;
                    else out = ep;
                }
                if (in != null && out != null) {
                    dados = itf;
                    entrada = in;
                    saida = out;
                }
            }
        }
        if (dados == null) {
            limpar();
            if (call != null) call.reject("O módulo não tem a porta serial (CDC) esperada.");
            return;
        }
        UsbDeviceConnection aberta = usb.openDevice(aparelho);
        if (aberta == null || !aberta.claimInterface(dados, true)) {
            if (aberta != null) aberta.close();
            limpar();
            if (call != null) call.reject("Não deu para abrir o módulo no USB-C.");
            return;
        }
        if (comunicacao != null) {
            aberta.claimInterface(comunicacao, true);
            aberta.controlTransfer(0x21, 0x20, 0, comunicacao.getId(), LINHA_115200, LINHA_115200.length, 1000);
            // DTR ligado e RTS desligado: assim o ESP32-C3 não reinicia ao abrir a porta.
            aberta.controlTransfer(0x21, 0x22, 0x01, comunicacao.getId(), null, 0, 1000);
        }
        conexao = aberta;
        lendo = true;
        final UsbDeviceConnection c = aberta;
        final UsbEndpoint in = entrada;
        new Thread(() -> ler(c, in), "ybyra-rfid-usb").start();
        avisar(true);
        if (call != null) call.resolve();
    }

    /** Lê a porta enquanto estiver aberta e manda cada linha completa para o app. */
    private void ler(UsbDeviceConnection c, UsbEndpoint in) {
        byte[] buffer = new byte[Math.max(64, in.getMaxPacketSize())];
        StringBuilder linha = new StringBuilder();
        while (lendo && conexao == c) {
            int n = c.bulkTransfer(in, buffer, buffer.length, 200);
            if (n <= 0) continue;
            for (int i = 0; i < n; i++) {
                char ch = (char) (buffer[i] & 0xFF);
                if (ch == '\n') {
                    String texto = linha.toString().trim();
                    linha.setLength(0);
                    if (!texto.isEmpty()) {
                        JSObject d = new JSObject();
                        d.put("texto", texto);
                        notifyListeners("linha", d);
                    }
                } else if (ch != '\r') {
                    linha.append(ch);
                }
            }
        }
    }

    private synchronized void fechar() {
        boolean estava = conexao != null;
        lendo = false;
        if (conexao != null) {
            if (dados != null) conexao.releaseInterface(dados);
            if (comunicacao != null) conexao.releaseInterface(comunicacao);
            conexao.close();
        }
        limpar();
        if (estava) avisar(false);
    }

    private void limpar() {
        conexao = null;
        comunicacao = null;
        dados = null;
        entrada = null;
        saida = null;
    }

    private void avisar(boolean conectado) {
        JSObject d = new JSObject();
        d.put("conectado", conectado);
        notifyListeners("conexao", d);
    }
}
