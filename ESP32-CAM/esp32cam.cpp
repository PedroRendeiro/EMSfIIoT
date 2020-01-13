#include "esp32cam.h"

/**
 * @brief Configuration function
 * 
 * Configures Serial communication, LEDs, WiFi and Camera
 * 
 * @return camera_config_t camera configuration struct
 */
camera_config_t ESP32CAM::config(void) {

  // Disable brownout detector
  WRITE_PERI_REG(RTC_CNTL_BROWN_OUT_REG, 0);

  // Initialize Serial communication
  Serial.begin(115200);
  Serial.println();

  // Initialize Flash LED
  pinMode(CAM_PIN_FLASH, OUTPUT);
  digitalWrite(CAM_PIN_FLASH, LOW);
  // Initialize BuiltIn LED
  pinMode(CAM_PIN_LED, OUTPUT);
  digitalWrite(CAM_PIN_LED, HIGH);

  // Configure WiFi communication
  connectWiFi();
  
  // Camera configuration
  _camera_config.pin_pwdn  = CAM_PIN_PWDN;
  _camera_config.pin_reset = CAM_PIN_RESET;
  _camera_config.pin_xclk = CAM_PIN_XCLK;
  _camera_config.pin_sscb_sda = CAM_PIN_SIOD;
  _camera_config.pin_sscb_scl = CAM_PIN_SIOC;
  
  _camera_config.pin_d7 = CAM_PIN_D7;
  _camera_config.pin_d6 = CAM_PIN_D6;
  _camera_config.pin_d5 = CAM_PIN_D5;
  _camera_config.pin_d4 = CAM_PIN_D4;
  _camera_config.pin_d3 = CAM_PIN_D3;
  _camera_config.pin_d2 = CAM_PIN_D2;
  _camera_config.pin_d1 = CAM_PIN_D1;
  _camera_config.pin_d0 = CAM_PIN_D0;
  _camera_config.pin_vsync = CAM_PIN_VSYNC;
  _camera_config.pin_href = CAM_PIN_HREF;
  _camera_config.pin_pclk = CAM_PIN_PCLK;
  
  //XCLK 20MHz or 10MHz for OV2640 double FPS (Experimental)
  _camera_config.xclk_freq_hz = 20000000;
  _camera_config.pixel_format = PIXFORMAT_JPEG;
  _camera_config.ledc_timer = LEDC_TIMER_0;
  _camera_config.ledc_channel = LEDC_CHANNEL_0;

  // Check if PSRAM is available
  if(psramFound()){
    _camera_config.frame_size = FRAMESIZE_UXGA; // FRAMESIZE_ + QVGA|CIF|VGA|SVGA|XGA|SXGA|UXGA
    _camera_config.jpeg_quality = 10;
    _camera_config.fb_count = 2;
    Serial.println("Camera with PSRAM detected!");
  } else {
    _camera_config.frame_size = FRAMESIZE_SVGA;
    _camera_config.jpeg_quality = 12;
    _camera_config.fb_count = 1;
  }

  // Return camera configuration struct
  return _camera_config;
}

/**
 * @brief WiFi connection function
 * 
 * Connects to WiFi network defined in header file
 * If connections takes to long resets the device
 * 
 * @return boolean true if connection OK
 */
boolean ESP32CAM::connectWiFi(void) {
  Serial.print("Connecting to WiFi\n...");
  digitalWrite(CAM_PIN_LED, LOW);
  WiFi.mode(WIFI_STA);
  WiFi.begin(_ssid, _password);
  uint32_t start = millis();
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
    if ((millis()-start)>20000) {
      digitalWrite(CAM_PIN_LED, HIGH);
      ESP.restart();
    }
  }
  Serial.println("\nWiFi connected");
  digitalWrite(CAM_PIN_LED, HIGH);
  return true;
}

/**
 * @brief Camera initialization function
 * 
 * Initializes Camera
 * 
 * @return esp_err_t
 */
esp_err_t ESP32CAM::init(void) {
  // Power up the camera if PWDN pin is defined
  if(CAM_PIN_PWDN != -1){
    pinMode(CAM_PIN_PWDN, OUTPUT);
    digitalWrite(CAM_PIN_PWDN, LOW);
  }

  // Initialize the camera
  esp_err_t err = esp_camera_init(&_camera_config);
  if (err != ESP_OK) {
    ESP_LOGE(TAG, "Camera Init Failed");
    return err;
  }

  // Return OK
  return ESP_OK;
}

/**
 * @brief Camera capture function
 * 
 * Captures a picture
 * 
 * @return esp_err_t 
 */
esp_err_t ESP32CAM::capture(void) {
  // Acquire a frame
  camera_fb_t * fb = esp_camera_fb_get();
  if (!fb) {
    ESP_LOGE(TAG, "Camera Capture Failed");
    return ESP_FAIL;
  }

  // Return the frame buffer back to the driver for reuse
  esp_camera_fb_return(fb);

  // Return OK
  return ESP_OK;
}

/**
 * @brief Function to acquire and send picture over HTTP
 * 
 * Initializes and checks luminosity sensor
 * If light too low -> Use flash
 * 
 * @param req 
 * @return esp_err_t 
 */
esp_err_t ESP32CAM::jpg_httpd_handler(httpd_req_t *req) {
  digitalWrite(CAM_PIN_LED, LOW);
  camera_fb_t * fb = NULL;
  esp_err_t res = ESP_OK;
  size_t fb_len = 0;
  int64_t fr_start = esp_timer_get_time();

  // Initialize light sensor
  TSL2591 lightSensor = TSL2591(I2C_ADDRESS, I2C_SDA, I2C_SCL);
  Serial.print("Connecting to I2C Light Sensor\n...");
  uint32_t start = millis();
  while (!lightSensor.begin()) {
    Serial.print(".");
    delay(500);
    if ((millis()-start)>5000) {
      //ESP.restart();
      break;
    }
  }
  Serial.println("\nI2C Light Sensor connected");
  
  // Check lux and light flash
  boolean flashOn = false;
  if (lightSensor.getLuminosity(TSL2591_VISIBLE) < 100) {
    digitalWrite(CAM_PIN_FLASH, HIGH);
    Serial.println("Flash On!");
    delay(1000);
    flashOn = true;
  }
  
  Serial.print("Capturing frame\n...");
  // Acquire a frame
  fb = esp_camera_fb_get();
  if (!fb) {
    ESP_LOGE(TAG, "Camera capture failed");
    httpd_resp_send_500(req);
    digitalWrite(CAM_PIN_LED, HIGH);
    return ESP_FAIL;
  }
  Serial.println("\nCapture done!");

  // If flash was used, turn off
  if (flashOn) {
    delay(1000);
    digitalWrite(CAM_PIN_FLASH, LOW);
    Serial.println("Flash Off!");
    flashOn = false;
  }

  Serial.println("Sending header");
  // Send response header
  res = httpd_resp_set_type(req, "image/jpeg");
  if(res == ESP_OK){
    res = httpd_resp_set_hdr(req, "Content-Disposition", "inline; filename=capture.jpg");
  }
  Serial.println("Sending frame");
  // Send response content
  if(res == ESP_OK){
    fb_len = fb->len;
    res = httpd_resp_send(req, (const char *)fb->buf, fb->len);
  }
  
  // Return the frame buffer back to the driver for reuse
  esp_camera_fb_return(fb);

  // Log everything
  int64_t fr_end = esp_timer_get_time();
  ESP_LOGI(TAG, "JPG: %uKB %ums", (uint32_t)(fb_len/1024), (uint32_t)((fr_end - fr_start)/1000));
  digitalWrite(CAM_PIN_LED, HIGH);

  // Return
  return res;
}

/**
 * @brief Function to send Camera status over HTTP
 * 
 * @param req 
 * @return esp_err_t 
 */
esp_err_t ESP32CAM::status_handler(httpd_req_t *req) {
  static char json_response[1024];

  sensor_t * s = esp_camera_sensor_get();
  char * p = json_response;
  *p++ = '{';

  p+=sprintf(p, "\"framesize\":%u,", s->status.framesize);
  p+=sprintf(p, "\"quality\":%u,", s->status.quality);
  p+=sprintf(p, "\"brightness\":%d,", s->status.brightness);
  p+=sprintf(p, "\"contrast\":%d,", s->status.contrast);
  p+=sprintf(p, "\"saturation\":%d,", s->status.saturation);
  p+=sprintf(p, "\"sharpness\":%d,", s->status.sharpness);
  p+=sprintf(p, "\"special_effect\":%u,", s->status.special_effect);
  p+=sprintf(p, "\"wb_mode\":%u,", s->status.wb_mode);
  p+=sprintf(p, "\"awb\":%u,", s->status.awb);
  p+=sprintf(p, "\"awb_gain\":%u,", s->status.awb_gain);
  p+=sprintf(p, "\"aec\":%u,", s->status.aec);
  p+=sprintf(p, "\"aec2\":%u,", s->status.aec2);
  p+=sprintf(p, "\"ae_level\":%d,", s->status.ae_level);
  p+=sprintf(p, "\"aec_value\":%u,", s->status.aec_value);
  p+=sprintf(p, "\"agc\":%u,", s->status.agc);
  p+=sprintf(p, "\"agc_gain\":%u,", s->status.agc_gain);
  p+=sprintf(p, "\"gainceiling\":%u,", s->status.gainceiling);
  p+=sprintf(p, "\"bpc\":%u,", s->status.bpc);
  p+=sprintf(p, "\"wpc\":%u,", s->status.wpc);
  p+=sprintf(p, "\"raw_gma\":%u,", s->status.raw_gma);
  p+=sprintf(p, "\"lenc\":%u,", s->status.lenc);
  p+=sprintf(p, "\"vflip\":%u,", s->status.vflip);
  p+=sprintf(p, "\"hmirror\":%u,", s->status.hmirror);
  p+=sprintf(p, "\"dcw\":%u,", s->status.dcw);
  p+=sprintf(p, "\"colorbar\":%u,", s->status.colorbar);
  *p++ = '}';
  *p++ = 0;
  httpd_resp_set_type(req, "application/json");
  httpd_resp_set_hdr(req, "Access-Control-Allow-Origin", "*");
  return httpd_resp_send(req, json_response, strlen(json_response));
}

/**
 * @brief Function to start server
 * 
 * Calls Camera configuration and initialization
 * Starts webserver
 * Waits for connections
 * 
 */
void ESP32CAM::startServer(void) {

  config();
  init();

  httpd_config_t config = HTTPD_DEFAULT_CONFIG();

  httpd_uri_t status_uri = {
    .uri       = "/status",
    .method    = HTTP_GET,
    .handler   = status_handler,
    .user_ctx  = NULL
  };

  httpd_uri_t capture_uri = {
    .uri       = "/capture",
    .method    = HTTP_GET,
    .handler   = jpg_httpd_handler,
    .user_ctx  = NULL
  };
  
  Serial.printf("Starting web server on port: '%d'\n", config.server_port);
  if (httpd_start(&_camera_httpd, &config) == ESP_OK) {
    httpd_register_uri_handler(_camera_httpd, &status_uri);
    httpd_register_uri_handler(_camera_httpd, &capture_uri);
  }
  Serial.print("Camera Ready! Use 'http://");
  Serial.print(WiFi.localIP());
  Serial.println("' to connect");
}