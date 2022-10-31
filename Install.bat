echo "Instructions from https://huggingface.co/docs/transformers/installation"

python -m venv ./venv

setlocal

cd venv
cd Scripts

pip3 install torch torchvision torchaudio --extra-index-url https://download.pytorch.org/whl/cu117

pip install flax

pip install transformers

python ../../install.py

endlocal
