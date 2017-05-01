package="/tmp/gablab.zip"
workspace="/tmp/gablab/"

sudo apt-get update
sudo apt-get install python3-numpy -qq --yes
sudo apt-get install python3-scipy -qq --yes
sudo apt-get install python3-matplotlib -qq --yes
sudo apt-get install python3-requests -qq --yes
sudo apt-get install ipython3 -qq --yes
sudo apt-get install unzip -qq --yes

if [ ! -d $workspace ]; then
  sudo mkdir $workspace
fi

if [ -f $package ]; then
  sudo unzip $package -d $workspace
fi

