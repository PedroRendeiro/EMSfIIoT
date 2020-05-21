import configparser, logging, requests, json, sys, base64

class Things():

    def __init__(self):
        self.log = logging.getLogger('EMSfIIoT')
        
        config = configparser.ConfigParser()
        config.read("cfg/emsfiiot.cfg")

        self.__thingId = config['hub']['thingId']
        
        self.__server = config['things']['server']
        self.__api_token = config['things']['api_token']

        self.__endpoint = config['OAuth2']['endpoint']
        self.__clientId = config['OAuth2']['clientId']
        self.__clientSecret = config['OAuth2']['clientSecret']
        self.__scope = config['OAuth2']['scope']

        payload = {'grant_type': 'client_credentials', 'client_id': self.__clientId, 'client_secret': self.__clientSecret, 'scope': self.__scope}
        response = requests.post(self.__endpoint, data=payload)
        if response.status_code == 200:
            self.__token = json.loads(response.content.decode("utf-8"))
        else:
            self.log.error("Status Code: " + str(response.status_code) + " | Body: " + response.content.decode('utf-8'))
            sys.exit(1)

        headers = {'Authorization': 'Bearer ' + self.__token['access_token']}
        response = requests.get(self.__server + "/things/" + self.__thingId + "/attributes/configuration", headers=headers)

        if response.status_code == 200:
            self.__configuration = json.loads(response.content.decode("utf-8"))
        else:
            self.log.error("Status Code: " + str(response.status_code) + " | Body: " + response.content.decode('utf-8'))
            sys.exit(1)
    
    def get(self):
        return self.__configuration

if __name__ == "__main__":
    Thing = Things()