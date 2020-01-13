# 1 "c:\\Users\\pedro\\OneDrive - Universidade de Aveiro\\5º ano\\DPE\\Software\\EMSfIIOT\\ESP32-CAM\\ESP32-CAM.ino"
# 2 "c:\\Users\\pedro\\OneDrive - Universidade de Aveiro\\5º ano\\DPE\\Software\\EMSfIIOT\\ESP32-CAM\\ESP32-CAM.ino" 2

ESP32CAM camera = ESP32CAM();

void setup() {
  camera.startServer();
}

void loop() {
  if (WiFi.status() != WL_CONNECTED) {
    camera.connectWiFi();
  }
  delay(10000);
}
