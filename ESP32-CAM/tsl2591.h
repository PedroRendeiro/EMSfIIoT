#ifndef _TSL2591_H_
#define _TSL2591_H_

#include <Arduino.h>
#include "Wire.h"

// Light sensor definitions
#define TSL2591_COMMAND_BIT       0xA0

#define TSL2591_VISIBLE           2       ///< (channel 0) - (channel 1)
#define TSL2591_INFRARED          1       ///< channel 1
#define TSL2591_FULLSPECTRUM      0       ///< channel 0

#define TSL2591_ENABLE_POWEROFF   0x00
#define TSL2591_ENABLE_POWERON    0x01
#define TSL2591_ENABLE_AEN        0x02
#define TSL2591_ENABLE_AIEN       0x10
#define TSL2591_ENABLE_NPIEN      0x80

#define TSL2591_LUX_DF            408.0F ///< Lux cooefficient

/// TSL2591 Register map
enum
{
  TSL2591_REGISTER_ENABLE             = 0x00, // Enable register
  TSL2591_REGISTER_CONTROL            = 0x01, // Control register
  TSL2591_REGISTER_THRESHOLD_AILTL    = 0x04, // ALS low threshold lower byte
  TSL2591_REGISTER_THRESHOLD_AILTH    = 0x05, // ALS low threshold upper byte
  TSL2591_REGISTER_THRESHOLD_AIHTL    = 0x06, // ALS high threshold lower byte
  TSL2591_REGISTER_THRESHOLD_AIHTH    = 0x07, // ALS high threshold upper byte
  TSL2591_REGISTER_THRESHOLD_NPAILTL  = 0x08, // No Persist ALS low threshold lower byte
  TSL2591_REGISTER_THRESHOLD_NPAILTH  = 0x09, // No Persist ALS low threshold higher byte
  TSL2591_REGISTER_THRESHOLD_NPAIHTL  = 0x0A, // No Persist ALS high threshold lower byte
  TSL2591_REGISTER_THRESHOLD_NPAIHTH  = 0x0B, // No Persist ALS high threshold higher byte
  TSL2591_REGISTER_PERSIST_FILTER     = 0x0C, // Interrupt persistence filter
  TSL2591_REGISTER_PACKAGE_PID        = 0x11, // Package Identification
  TSL2591_REGISTER_DEVICE_ID          = 0x12, // Device Identification
  TSL2591_REGISTER_DEVICE_STATUS      = 0x13, // Internal Status
  TSL2591_REGISTER_CHAN0_LOW          = 0x14, // Channel 0 data, low byte
  TSL2591_REGISTER_CHAN0_HIGH         = 0x15, // Channel 0 data, high byte
  TSL2591_REGISTER_CHAN1_LOW          = 0x16, // Channel 1 data, low byte
  TSL2591_REGISTER_CHAN1_HIGH         = 0x17, // Channel 1 data, high byte
};

/// Enumeration for the sensor integration timing
typedef enum
{
  TSL2591_INTEGRATIONTIME_100MS     = 0x00,  // 100 millis
  TSL2591_INTEGRATIONTIME_200MS     = 0x01,  // 200 millis
  TSL2591_INTEGRATIONTIME_300MS     = 0x02,  // 300 millis
  TSL2591_INTEGRATIONTIME_400MS     = 0x03,  // 400 millis
  TSL2591_INTEGRATIONTIME_500MS     = 0x04,  // 500 millis
  TSL2591_INTEGRATIONTIME_600MS     = 0x05,  // 600 millis
}
tsl2591IntegrationTime_t;

/// Enumeration for the sensor gain
typedef enum
{
  TSL2591_GAIN_LOW                  = 0x00,    /// low gain (1x)
  TSL2591_GAIN_MED                  = 0x10,    /// medium gain (25x)
  TSL2591_GAIN_HIGH                 = 0x20,    /// medium gain (428x)
  TSL2591_GAIN_MAX                  = 0x30,    /// max gain (9876x)
}
tsl2591Gain_t;

class TSL2591 {
  public:
    TSL2591(uint8_t i2cAddress, uint8_t sdaPin, uint8_t sclPin);
    boolean begin(void);
    void enable(void);
    void disable(void);

    uint16_t getLuminosity(uint8_t channel);
    uint32_t getFullLuminosity(void);
    float calculateLux(uint16_t ch0, uint16_t ch1);

    void setGain(tsl2591Gain_t gain);
    void setTiming(tsl2591IntegrationTime_t integration);
    tsl2591IntegrationTime_t getTiming();
    tsl2591Gain_t getGain();

    uint8_t getStatus(void);

    unsigned char readByte(unsigned char address);
    boolean writeByte(unsigned char address, unsigned char value);
    uint8_t readUInt(unsigned char address);
  
  private:
    uint8_t _i2cAddress;
    uint8_t _sdaPin;
    uint8_t _sclPin;

    tsl2591IntegrationTime_t _integration;
    tsl2591Gain_t _gain;
    int32_t _sensorID;
    
    boolean _initialized;
};

#endif
