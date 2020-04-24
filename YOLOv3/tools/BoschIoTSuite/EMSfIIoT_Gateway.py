# // Generated by Vorto from com.bosch.iot.suite.example.octopussuiteedition.GenericSensor

class EMSfIIoT_Gateway(object):
    def __init__(self):
        self.value = 0.0
        self.unit = ""
        self.timeStamp = ""
        self.measureTypeID = 0.0
        self.locationID = 0.0

    ### Status property value
    @property
    def value(self):
        return self.__value[0]
    
    @value.setter
    def value(self, value):
        self.__value = (value, True)
    
    ### Status property unit
    @property
    def unit(self):
        return self.__unit[0]
    
    @unit.setter
    def unit(self, value):
        self.__unit = (value, True)
    
    ### Status property timeStamp
    @property
    def timeStamp(self):
        return self.__timeStamp[0]
    
    @timeStamp.setter
    def timeStamp(self, value):
        self.__timeStamp = (value, True) 

    ### Status property measureTypeID
    @property
    def measureTypeID(self):
        return self.__measureTypeID[0]
    
    @measureTypeID.setter
    def measureTypeID(self, value):
        self.__measureTypeID = (value, True)

    ### Status property locationID
    @property
    def locationID(self):
        return self.__locationID[0]
    
    @locationID.setter
    def locationID(self, value):
        self.__locationID = (value, True)  
    
    def serializeStatus(self, serializer):
        serializer.first_prop = True
        if self.__value[1]:
               serializer.serialize_property("value", self.__value[0])
               self.__value = (self.__value[0], False)
        if self.__unit[1]:
               serializer.serialize_property("unit", self.__unit[0])
               self.__unit = (self.__unit[0], False)
        if self.__timeStamp[1]:
               serializer.serialize_property("timeStamp", self.__timeStamp[0])
               self.__timeStamp = (self.__timeStamp[0], False)
        if self.__measureTypeID[1]:
               serializer.serialize_property("measureTypeID", self.__measureTypeID[0])
               self.__measureTypeID = (self.__measureTypeID[0], False)
        if self.__locationID[1]:
               serializer.serialize_property("locationID", self.__locationID[0])
               self.__locationID = (self.__locationID[0], False)
    def serializeConfiguration(self, serializer):
        pass
        #serializer.first_prop = True
        #if self.__applicationType[1]:
        #       serializer.serialize_property("applicationType", self.__applicationType[0])
        #       self.__applicationType = (self.__applicationType[0], False)
