import requests
import os

os.system('python3 /tmp/gablab/seliga.py < seliga.in')

with open('results.dat', 'r') as content_file:
  content = content_file.read()

url = 'http://gab2017.trafficmanager.net/api/Batch/UploadOutput?inputId=' + os.environ['INPUT_ID'] +  '&email='+ os.environ['USER_EMAIL']
payload = {'content': content}

r = requests.post(url, json=payload)

