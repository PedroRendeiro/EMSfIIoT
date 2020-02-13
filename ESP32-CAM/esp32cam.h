#ifndef _ESP32CAM_H_
#define _ESP32CAM_H_

#include <Arduino.h>

#include "esp_camera.h"
#include "esp_http_server.h"
#include "esp_timer.h"

#include "soc/soc.h"            // Disable brownour problems
#include "soc/rtc_cntl_reg.h"   // Disable brownour problems

#include "tsl2591.h"
#include "WiFi.h"

// Pin definition for I2C communication
#define I2C_SDA                   13
#define I2C_SCL                   14
#define I2C_ADDRESS               0x29

// WiFi definitions
//#define SSID                      "AndroidAP6120"
//#define PASSWORD                  "4f5cd3453ae3"
//#define SSID                      "DESKTOP-CSTFN6E"
//#define PASSWORD                  "r678Q,03"
#define SSID                        "sala"
#define PASSWORD                    "wfgysf5657"
#define SSID                        "TP-Link_BD6B"
#define PASSWORD                    "31192996"

// Pin definition for CAMERA_MODEL_AI_THINKER
#define CAM_PIN_PWDN              32
#define CAM_PIN_RESET             -1
#define CAM_PIN_XCLK              0
#define CAM_PIN_SIOD              26
#define CAM_PIN_SIOC              27

#define CAM_PIN_FLASH             4
#define CAM_PIN_LED               33

#define CAM_PIN_D7                35
#define CAM_PIN_D6                34
#define CAM_PIN_D5                39
#define CAM_PIN_D4                36
#define CAM_PIN_D3                21
#define CAM_PIN_D2                19
#define CAM_PIN_D1                18
#define CAM_PIN_D0                5
#define CAM_PIN_VSYNC             25
#define CAM_PIN_HREF              23
#define CAM_PIN_PCLK              22

class ESP32CAM {
  public:
    ESP32CAM(){};
    camera_config_t config(void);
    esp_err_t init(void);
    esp_err_t capture(void);

    void startServer(void);

    boolean connectWiFi(void);
  private:
    static esp_err_t jpg_httpd_handler(httpd_req_t *req);
    static esp_err_t jpg_httpd_handler_with_flash(httpd_req_t *req);
    static esp_err_t jpg_httpd_handler_without_flash(httpd_req_t *req);
    static esp_err_t status_handler(httpd_req_t *req);

    camera_config_t _camera_config;
    httpd_handle_t _camera_httpd;

    const char* _ssid = SSID;
    const char* _password = PASSWORD;
};

#endif
