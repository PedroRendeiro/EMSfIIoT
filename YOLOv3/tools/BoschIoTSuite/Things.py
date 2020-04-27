import configparser, logging, requests, json, sys
import base64

class Things():

    def __init__(self):
        self.log = logging.getLogger('EMSfIIoT')
        
        config = configparser.ConfigParser()
        config.read("cfg/emsfiiot.cfg")

        self.__server = config['things']['server']
        self.__endpoint = config['things']['endpoint']
        self.__api_token = config['things']['api_token']

        self.__imTenantName = config['permissions']['imTenantName']
        self.__username = config['permissions']['username']
        self.__password = config['permissions']['password']

        auth = self.__imTenantName + '\\' + self.__username + ":" + self.__password
        auth_bytes = auth.encode('ascii')
        base64_bytes = base64.b64encode(auth_bytes)
        base64_auth = base64_bytes.decode('ascii')

        headers = {'Authorization': 'Basic ' + base64_auth, 'x-cr-api-token': self.__api_token}
        response = requests.get(self.__server + self.__endpoint, headers=headers)

        if response.status_code == 200:
            self.__devices = json.loads(response.content.decode("utf-8"))
        else:
            self.log.error("Status Code: " + str(response.status_code) + " | Body: " + response.content.decode('utf-8'))
            sys.exit(1)
    
    def get(self):
        return self.__devices

if __name__ == "__main__":
    Thing = Things()