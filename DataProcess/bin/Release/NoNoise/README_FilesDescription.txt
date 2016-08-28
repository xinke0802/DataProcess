This file describes the folders and files in this folder. Every important sub-folder will also has a files description README in themselves.

Files Description
==========================
## Files in this folder are similarity matrices of non-noisy tweet cluster produced by original framework of paper "Enquiring Minds Early Detection of Rumors in Social Media from Enquiry Posts". There are 214 non-noisy original clusters in total and they are from data subset in Nov, 2014. If and only if a original tweet cluster belong to a real cluster with more than 9 tweets, it will be defined as "non-noisy." The similarity matrices are used for secondary clustering.

label_cluster.txt
    Ground truth label files for secondary clustering. The original tweet clusters with same label numbers belong to the same real cluster.

clusterTimeSimilarity.txt
    Time similarity matrix.
    
clusterHashtagSimilarity.txt
    Hashtag similarity matrix.
    
clusterNameEntitySetSimilarity
    Name-entity similarity matrix.

clusterMentionSimilarity.txt
    User mention similarity matrix.
    
clusterWordJaccardSimilarity.txt
    Word set jaccard similarity matrix.
    
clusterTfIdfSimilarity.txt
    Tf-idf similarity similarity matrix.
    
clusterCmSimilarity.txt
    One of the baseline semantic similarity matrix, which is produced by the CM algorithm from package SEMILAR.

clusterGreedySimilarity.txt
    One of the baseline semantic similarity matrix, which is produced by the greedy algorithm from package SEMILAR.
