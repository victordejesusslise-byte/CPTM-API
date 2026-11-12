package br.cptm.ybyra;

import android.os.Bundle;
import com.getcapacitor.BridgeActivity;

public class MainActivity extends BridgeActivity {
    @Override
    public void onCreate(Bundle savedInstanceState) {
        // O módulo RFID no USB-C (YBYRA-RFID-P0): plugin do próprio app, registrado antes de a ponte subir.
        registerPlugin(RfidUsbPlugin.class);
        super.onCreate(savedInstanceState);
    }
}
