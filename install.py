from constants import repoUri, cacheDir

from transformers import AutoModelForCausalLM, AutoTokenizer

print("Repo URI: {0}".format(repoUri))
print("Cache Dir: {0}".format(cacheDir))

AutoTokenizer.from_pretrained(repoUri, cache_dir=cacheDir)
AutoModelForCausalLM.from_pretrained(repoUri)
