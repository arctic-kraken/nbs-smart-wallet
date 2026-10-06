#!/bin/sh

set -e

echo "$ob_iss_ca_cert" > /usr/local/share/ca-certificates/ob_iss_ca_cert.crt

echo "$ob_root_ca_cert" > /usr/local/share/ca-certificates/ob_root_ca_cert.crt

chmod 644 /usr/local/share/ca-certificates/ob_iss_ca_cert.crt

chmod 644 /usr/local/share/ca-certificates/ob_root_ca_cert.crt

update-ca-certificates

exec "$@"