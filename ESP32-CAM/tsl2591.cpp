#include "Arduino.h"
#include "tsl2591.h"

/**
 * @brief Construct a new TSL2591::TSL2591 object
 * 
 * @param i2cAddress 
 * @param sdaPin 
 * @param sclPin 
 */
TSL2591::TSL2591(uint8_t i2cAddress, uint8_t sdaPin, uint8_t sclPin) {
  _initialized = false;
  _integration = TSL2591_INTEGRATIONTIME_100MS;
  _gain        = TSL2591_GAIN_MED;

  _i2cAddress = i2cAddress;
  _sdaPin = sdaPin;
  _sclPin = sclPin;
  // Can't initialize now because Wire.h not included yet
}

/**
 * @brief Starts I2C communication with sensor
 * 
 * Checks device ID
 * Sets default integration time and gain
 * 
 * @return boolean 
 */
boolean TSL2591::begin(void) {
  // Use 400kHz I2C frequency
  Wire.setClock(400000);
  //Start I2C communication
  Wire.begin(_sdaPin, _sclPin);

  //Check if proper device is connected
  uint8_t id = readUInt(TSL2591_REGISTER_DEVICE_ID);
  if (id != 0x50 ) {
    return false;
  }

  _initialized = true;

  // Set default integration time and gain
  setTiming(_integration);
  setGain(_gain);

  // Disable the device
  disable();

  return true;
}

/**
 * @brief Enables device
 * 
 * PowerOn + ADC + Interrupts
 * 
 */
void TSL2591::enable(void) {
  if (!_initialized)
  {
    if (!begin()) {
      return;
    }
  }

  // Enable the device
  writeByte(TSL2591_REGISTER_ENABLE, TSL2591_ENABLE_POWERON | TSL2591_ENABLE_AEN | TSL2591_ENABLE_AIEN | TSL2591_ENABLE_NPIEN);
}

/**
 * @brief Disables the device
 * 
 * PowerOff
 * 
 */
void TSL2591::disable(void) {
  if (!_initialized)
  {
    if (!begin()) {
      return;
    }
  }

  // Disable the device
  writeByte(TSL2591_REGISTER_ENABLE, TSL2591_ENABLE_POWEROFF);
}

/**
 * @brief Set light sensor gain
 * 
 * @param gain 
 */
void TSL2591::setGain(tsl2591Gain_t gain)
{
  if (!_initialized) {
    if (!begin()) {
      return;
    }
  }

  enable();
  _gain = gain;
  writeByte(TSL2591_REGISTER_CONTROL, _integration | _gain);
  disable();
}

/**
 * @brief Get light sensor gain
 * 
 * Necessary beacause class member _gain is private
 * 
 * @return tsl2591Gain_t 
 */
tsl2591Gain_t TSL2591::getGain()
{
  return _gain;
}

/**
 * @brief Set light sensor integration time
 * 
 * @param integration 
 */
void TSL2591::setTiming(tsl2591IntegrationTime_t integration)
{
  if (!_initialized) {
    if (!begin()) {
      return;
    }
  }

  enable();
  _integration = integration;
  writeByte(TSL2591_REGISTER_CONTROL, _integration | _gain);
  disable();
}

/**
 * @brief Get light sensor integration time
 * 
 * Necessary beacause class member _integration is private
 * 
 * @return tsl2591IntegrationTime_t 
 */
tsl2591IntegrationTime_t TSL2591::getTiming()
{
  return _integration;
}

/**
 * @brief Calculates lux value from both channels (Visible and IR) readings
 * 
 * @param ch0 
 * @param ch1 
 * @return float 
 */
float TSL2591::calculateLux(uint16_t ch0, uint16_t ch1)
{
  float    atime, again;
  float    cpl, lux1, lux2, lux;
  uint32_t chan0, chan1;

  // Check for overflow conditions first
  if ((ch0 == 0xFFFF) | (ch1 == 0xFFFF))
  {
    // Signal an overflow
    return -1;
  }

  // Note: This algorithm is based on preliminary coefficients
  // provided by AMS and may need to be updated in the future

  switch (_integration)
  {
    case TSL2591_INTEGRATIONTIME_100MS :
      atime = 100.0F;
      break;
    case TSL2591_INTEGRATIONTIME_200MS :
      atime = 200.0F;
      break;
    case TSL2591_INTEGRATIONTIME_300MS :
      atime = 300.0F;
      break;
    case TSL2591_INTEGRATIONTIME_400MS :
      atime = 400.0F;
      break;
    case TSL2591_INTEGRATIONTIME_500MS :
      atime = 500.0F;
      break;
    case TSL2591_INTEGRATIONTIME_600MS :
      atime = 600.0F;
      break;
    default: // 100ms
      atime = 100.0F;
      break;
  }

  switch (_gain)
  {
    case TSL2591_GAIN_LOW :
      again = 1.0F;
      break;
    case TSL2591_GAIN_MED :
      again = 25.0F;
      break;
    case TSL2591_GAIN_HIGH :
      again = 428.0F;
      break;
    case TSL2591_GAIN_MAX :
      again = 9876.0F;
      break;
    default: // MED
      again = 25.0F;
      break;
  }

  cpl = (atime * again) / TSL2591_LUX_DF;

  lux = ( ((float)ch0 - (float)ch1 )) * (1.0F - ((float)ch1/(float)ch0) ) / cpl;

  // Return lux value
  return lux;
}

/**
 * @brief Reads luminosity from both channels (Visible and IR)
 * 
 * @return uint32_t 
 */
uint32_t TSL2591::getFullLuminosity(void)
{
  if (!_initialized) {
    if (!begin()) {
      return 0;
    }
  }

  // Enable the device
  enable();

  // Wait x ms for ADC to complete
  for (uint8_t d=0; d<=_integration; d++)
  {
    delay(120);
  }

  // CHAN0 must be read before CHAN1
  uint16_t y = readUInt(TSL2591_REGISTER_CHAN0_LOW);
  uint32_t x = readUInt(TSL2591_REGISTER_CHAN1_LOW);
  x <<= 16;
  x |= y;

  disable();

  return x;
}

/**
 * @brief Reads luminosity from selected channel (Visible and/or IR)
 * 
 * @param channel 
 * @return uint16_t 
 */
uint16_t TSL2591::getLuminosity(uint8_t channel)
{
  uint32_t x = getFullLuminosity();

  switch (channel) {
    case TSL2591_FULLSPECTRUM :
      // Reads two byte value from channel 0 (visible + infrared)
      return (x & 0xFFFF);
    case TSL2591_INFRARED :
      // Reads two byte value from channel 1 (infrared)
      return (x >> 16);
    case TSL2591_VISIBLE :
      // Reads all and subtracts out just the visible!
      return ((x & 0xFFFF) - (x >> 16));
    default :
      // unknown channel!
      return 0;
  }
}

/**
 * @brief Reads the device status byte
 * 
 * @return uint8_t 
 */
uint8_t TSL2591::getStatus(void)
{
  if (!_initialized) {
    if (!begin()) {
      return 0;
    }
  }

  // Enable the device
  enable();
  
  uint8_t x = readUInt(TSL2591_REGISTER_DEVICE_STATUS);
  
  disable();
  
  return x;
}

/**
 * @brief Read  byte as char from I2C
 * 
 * @param address 
 * @return unsigned char 
 */
unsigned char TSL2591::readByte(unsigned char address) {
  // Set up command byte for read
  Wire.beginTransmission(_i2cAddress);
  Wire.write(TSL2591_COMMAND_BIT | address);
  int error = Wire.endTransmission();

  // Read requested byte
  if (error == 0)
  {
    Wire.requestFrom((int)_i2cAddress,(int)1);
    if (Wire.available() == 1)
    {
      unsigned char value = Wire.read();
      return value;
    }
  }
  return false;
}

/**
 * @brief Write byte to I2C
 * 
 * @param address 
 * @param value 
 * @return boolean 
 */
boolean TSL2591::writeByte(unsigned char address, unsigned char value) {
  // Set up command byte for write
  Wire.beginTransmission(_i2cAddress);
  Wire.write(TSL2591_COMMAND_BIT | address);
  // Write byte
  Wire.write(value);
  int error = Wire.endTransmission();
  if (error == 0)
    return true;

  return false;
}

/**
 * @brief Read two bytes as integer from I2C
 * 
 * @param address 
 * @return uint8_t 
 */
uint8_t TSL2591::readUInt(unsigned char address) {
  char high, low;
    
  // Set up command byte for read
  Wire.beginTransmission(_i2cAddress);
  Wire.write(TSL2591_COMMAND_BIT | address);
  int error = Wire.endTransmission();

  // Read two bytes (low and high)
  if (error == 0)
  {
    Wire.requestFrom((int)_i2cAddress,(int)2);
    if (Wire.available() == 2)
    {
      low = Wire.read();
      high = Wire.read();
      // Combine bytes into unsigned int
      uint8_t value = word(high,low);
      return value;
    }
  }
  return false;
}