#include <Arduino.h>
#line 1 "c:\\Users\\pedro\\OneDrive - Universidade de Aveiro\\5º ano\\DPE\\Software\\EMSfIIOT\\ESP32-CAM\\ESP32-CAM.ino"
#include "esp32cam.h"

ESP32CAM camera = ESP32CAM();

#line 5 "c:\\Users\\pedro\\OneDrive - Universidade de Aveiro\\5º ano\\DPE\\Software\\EMSfIIOT\\ESP32-CAM\\ESP32-CAM.ino"
void setup();
#line 9 "c:\\Users\\pedro\\OneDrive - Universidade de Aveiro\\5º ano\\DPE\\Software\\EMSfIIOT\\ESP32-CAM\\ESP32-CAM.ino"
void loop();
#line 5 "c:\\Users\\pedro\\OneDrive - Universidade de Aveiro\\5º ano\\DPE\\Software\\EMSfIIOT\\ESP32-CAM\\ESP32-CAM.ino"
void setup() {
  camera.startServer();
}

void loop() {
  if (WiFi.status() != WL_CONNECTED) {
    camera.connectWiFi();
  }
  delay(10000);
}
