#include "esp32cam.h"

ESP32CAM camera = ESP32CAM();

void setup() {
  camera.startServer();
}

void loop() {
  if (WiFi.status() != WL_CONNECTED) {
    camera.connectWiFi();
  }
  delay(1);
}